using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CustomQotd.Database.DatabaseValues;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace CustomQotd.Database
{
    public static class DatabaseApi
    {
        private static readonly string connectionString = "Data Source=Database/database.db";

        /// <summary>
        /// Initializes the database and creates the necessary tables if they don't exist
        /// </summary>
        public static async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            //await ResetDatabaseAsync();

            var command = connection.CreateCommand();
            command.CommandText = ConfigTable;

            await command.ExecuteNonQueryAsync();
        }

        public static async Task ResetDatabaseAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DROP TABLE Config;
            ";

            await command.ExecuteNonQueryAsync();
        }

        public static async Task InitializeConfigAsync(ulong guildId, Dictionary<ConfigType, object?> values)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT OR REPLACE INTO Config (GuildId, {ConfigTypes})
                VALUES (@GuildId, {ConfigTypesParameters});
            ";

            command.Parameters.AddWithValue("@GuildId", guildId);
            AddParameters(command, values);
            
            await command.ExecuteNonQueryAsync();
        }

        public static async Task SetConfigAsync(ulong guildId, ConfigType configType, object? value)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Check if the entry exists
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"
                SELECT COUNT(1)
                FROM Config
                WHERE GuildId = @guildId;
            ";
            checkCommand.Parameters.AddWithValue("@guildId", guildId);

            var exists = (long)await checkCommand.ExecuteScalarAsync() > 0;

            if (!exists)
            {
                throw new DatabaseException($"No configuration found for GuildId `{guildId}`. Use `/initialize` to initialize.");
            }

            var command = connection.CreateCommand();
            string columnName = configType.ToString();

            if (!columnName.All(char.IsLetter))
                throw new DatabaseException("Config type must only be made of letters");

            command.CommandText = $@"
                UPDATE Config
                SET {columnName} = @value
                WHERE GuildId = @guildId;
            ";

            command.Parameters.AddWithValue("@value", value ?? DBNull.Value);
            command.Parameters.AddWithValue("@guildId", guildId);

            await command.ExecuteNonQueryAsync();
        }
        public static async Task<Dictionary<ConfigType, object?>> GetConfigAsync(ulong guildId)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {ConfigTypes}
                FROM Config
                WHERE GuildId = @guildId;
            ";

            command.Parameters.AddWithValue("@guildId", guildId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var configValues = new Dictionary<ConfigType, object?>();

                int index = 0;
                foreach (ConfigType configType in Enum.GetValues(typeof(ConfigType)))
                {
                    if (configType == ConfigType.GuildId)
                        continue;

                    configValues.Add(configType, GetConfigParameter(reader, configType, index));
                    index++;
                }

                return configValues;
            }
            else
            {
                throw new DatabaseException($"No configuration found for GuildId `{guildId}`. Use `/initialize` to initialize.");
            }
        }
        public static async Task<object?> GetConfigValueAsync(ulong guildId, ConfigType configType)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();

            string columnName = configType.ToString();

            if (!columnName.All(char.IsLetter))
                throw new DatabaseException("Config type must only be made of letters");

            command.CommandText = @$"
                SELECT {columnName}
                FROM Config
                WHERE GuildId = @guildId;
            ";

            command.Parameters.AddWithValue("@guildId", guildId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (configType == ConfigType.GuildId)
                    return reader.GetString(0);

                return GetConfigParameter(reader, configType, 0);
            }
            else
            {
                throw new DatabaseException($"No configuration found for GuildId `{guildId}`. Use `/initialize` to initialize.");
            }
        }
    }
}
