using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomQotd.Database
{
    public static class DatabaseHelper
    {
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
                CREATE TABLE Config (
                    GuildId INTEGER PRIMARY KEY,
                    AdminRoleId INTEGER NOT NULL,
                    QotdChannelId INTEGER NOT NULL,
                    QotdPingRoleId INTEGER NOT NULL,
                    QotdTimeHourUtc INTEGER NOT NULL,
                    QotdTimeMinuteUtc INTEGER NOT NULL,
                    SuggestionPingRoleId INTEGER NOT NULL,
                    SuggestionChannelId INTEGER NOT NULL
                );
            ";

            await command.ExecuteNonQueryAsync();
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
