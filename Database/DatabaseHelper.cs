using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CustomQotd.Features.Commands.ConfigCommand;
using System.Linq;

namespace CustomQotd.Database
{
    public static class DatabaseHelper
    {
        public class Exception(string message) : System.Exception
        {
            public new string Message { get; set; } = message;
        }

        private static readonly string connectionString = "Data Source=Database/database.db";

        /// <summary>
        /// Initializes the database and creates the necessary tables if they don't exist
        /// </summary>
        public static async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Config (
                    GuildId TEXT PRIMARY KEY,
                    AdminRoleId TEXT NOT NULL,
                    QotdChannelId TEXT NOT NULL,
                    QotdPingRoleId TEXT,
                    QotdTimeHourUtc INTEGER NOT NULL,
                    QotdTimeMinuteUtc INTEGER NOT NULL,
                    SuggestionPingRoleId TEXT,
                    SuggestionChannelId TEXT
                );
            ";

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
            command.CommandText = @"
                INSERT OR REPLACE INTO Config (GuildId, AdminRoleId, QotdChannelId, QotdPingRoleId, QotdTimeHourUtc, QotdTimeMinuteUtc, SuggestionPingRoleId, SuggestionChannelId)
                VALUES (@GuildId, @AdminRoleId, @QotdChannelId, @QotdPingRoleId, @QotdTimeHourUtc, @QotdTimeMinuteUtc, @SuggestionPingRoleId, @SuggestionChannelId);
            ";

            command.Parameters.AddWithValue("@GuildId", guildId);
            command.Parameters.AddWithValue("@AdminRoleId", values[ConfigType.AdminRoleId]);
            command.Parameters.AddWithValue("@QotdChannelId", values[ConfigType.QotdChannelId]);
            command.Parameters.AddWithValue("@QotdPingRoleId", values.ContainsKey(ConfigType.QotdPingRoleId) ? values[ConfigType.QotdPingRoleId] ?? DBNull.Value : DBNull.Value);
            command.Parameters.AddWithValue("@QotdTimeHourUtc", values[ConfigType.QotdTimeHourUtc]);
            command.Parameters.AddWithValue("@QotdTimeMinuteUtc", values[ConfigType.QotdTimeMinuteUtc]);
            command.Parameters.AddWithValue("@SuggestionPingRoleId", values.ContainsKey(ConfigType.SuggestionPingRoleId) ? values[ConfigType.SuggestionPingRoleId] ?? DBNull.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SuggestionChannelId", values.ContainsKey(ConfigType.SuggestionChannelId) ? values[ConfigType.SuggestionChannelId] ?? DBNull.Value : DBNull.Value);

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
                throw new Exception($"No configuration found for GuildId {guildId}. Use `/initialize` to initialize.");
            }

            var command = connection.CreateCommand();
            string columnName = configType.ToString();

            if (!columnName.All(char.IsLetter))
                throw new Exception("Config type must only be made of letters");

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
            command.CommandText = @"
                SELECT AdminRoleId, QotdChannelId, QotdPingRoleId, QotdTimeHourUtc, QotdTimeMinuteUtc, SuggestionPingRoleId, SuggestionChannelId
                FROM Config
                WHERE GuildId = @guildId;
            ";

            command.Parameters.AddWithValue("@guildId", guildId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var configValues = new Dictionary<ConfigType, object?>
                {
                    { ConfigType.AdminRoleId, reader.GetString(0) },
                    { ConfigType.QotdChannelId, reader.GetString(1) },
                    { ConfigType.QotdPingRoleId, reader.IsDBNull(2) ? null : reader.GetString(2) },
                    { ConfigType.QotdTimeHourUtc,reader.GetInt32(3) },
                    { ConfigType.QotdTimeMinuteUtc, reader.GetInt32(4) },
                    { ConfigType.SuggestionPingRoleId, reader.IsDBNull(5) ? null : reader.GetString(5) },
                    { ConfigType.SuggestionChannelId, reader.IsDBNull(6) ? null : reader.GetString(6) }
                };

                return configValues;
            }
            else
            {
                throw new Exception($"No configuration found for GuildId {guildId}. Use `/initialize` to initialize.");
            }
        }

        /// <summary>
        /// Logs a command execution to the database
        /// </summary>
        public static async Task LogCommandAsync(string userId, string command)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var commandText = connection.CreateCommand();
            commandText.CommandText = @"
                INSERT INTO CommandLogs (UserId, Command)
                VALUES ($userId, $command);
            ";

            commandText.Parameters.AddWithValue("$userId", userId);
            commandText.Parameters.AddWithValue("$command", command);

            await commandText.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves all command logs from the database
        /// </summary>
        public static async Task<List<CommandLog>> GetCommandLogsAsync()
        {
            var logs = new List<CommandLog>();

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, UserId, Command, Timestamp
                FROM CommandLogs
                ORDER BY Timestamp DESC;
            ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new CommandLog
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    Command = reader.GetString(2),
                    Timestamp = reader.GetDateTime(3)
                });
            }

            return logs;
        }
    }

    /// <summary>
    /// Represents a log entry for a command execution
    /// </summary>
    public class CommandLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
