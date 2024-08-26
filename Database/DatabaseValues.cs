﻿using Microsoft.Data.Sqlite;
using static CustomQotd.Features.Commands.ConfigCommand;

namespace CustomQotd.Database
{
    public static class DatabaseValues
    {
        #region Config
        public enum ConfigType
        {
            GuildId,
            BasicRoleId,
            AdminRoleId,
            QotdChannelId,
            QotdPingRoleId,
            QotdTimeHourUtc,
            QotdTimeMinuteUtc,
            SuggestionChannelId,
            SuggestionPingRoleId,
            LogsChannelId,
        }

        public const string ConfigTable = @"
                CREATE TABLE IF NOT EXISTS Config (
                    GuildId TEXT PRIMARY KEY,
                    BasicRoleId TEXT,
                    AdminRoleId TEXT NOT NULL,
                    QotdChannelId TEXT NOT NULL,
                    QotdPingRoleId TEXT,
                    QotdTimeHourUtc INTEGER NOT NULL,
                    QotdTimeMinuteUtc INTEGER NOT NULL,
                    SuggestionPingRoleId TEXT,
                    SuggestionChannelId TEXT,
                    LogsChannelId TEXT
                );
            ";
        public const string ConfigTypes = 
            @"BasicRoleId, AdminRoleId, QotdChannelId, QotdPingRoleId, QotdTimeHourUtc, QotdTimeMinuteUtc, SuggestionPingRoleId, SuggestionChannelId, LogsChannelId";
        public const string ConfigTypesParameters = 
            @"@BasicRoleId, @AdminRoleId, @QotdChannelId, @QotdPingRoleId, @QotdTimeHourUtc, @QotdTimeMinuteUtc, @SuggestionPingRoleId, @SuggestionChannelId, @LogsChannelId";

        public static void AddConfigParameters(SqliteCommand command, Dictionary<ConfigType, object?> values)
        {
            command.Parameters.AddWithValue("@BasicRoleId", values[ConfigType.BasicRoleId] ?? DBNull.Value);
            command.Parameters.AddWithValue("@AdminRoleId", values[ConfigType.AdminRoleId]);
            command.Parameters.AddWithValue("@QotdChannelId", values[ConfigType.QotdChannelId]);
            command.Parameters.AddWithValue("@QotdPingRoleId", values[ConfigType.QotdPingRoleId] ?? DBNull.Value);
            command.Parameters.AddWithValue("@QotdTimeHourUtc", values[ConfigType.QotdTimeHourUtc]);
            command.Parameters.AddWithValue("@QotdTimeMinuteUtc", values[ConfigType.QotdTimeMinuteUtc]);
            command.Parameters.AddWithValue("@SuggestionPingRoleId", values[ConfigType.SuggestionPingRoleId] ?? DBNull.Value);
            command.Parameters.AddWithValue("@SuggestionChannelId", values[ConfigType.SuggestionChannelId] ?? DBNull.Value);
            command.Parameters.AddWithValue("@LogsChannelId", values[ConfigType.LogsChannelId] ?? DBNull.Value);
        }

        public static object? GetConfigParameter(SqliteDataReader reader, ConfigType configType, int index)
        {
            switch (configType)
            {
                case ConfigType.BasicRoleId:
                    return reader.IsDBNull(index) ? null : reader.GetString(index);
                case ConfigType.AdminRoleId:
                    return reader.GetString(index);
                case ConfigType.QotdChannelId:
                    return reader.GetString(index);
                case ConfigType.QotdPingRoleId:
                    return reader.IsDBNull(index) ? null : reader.GetString(index);
                case ConfigType.QotdTimeHourUtc:
                    return reader.GetInt32(index);
                case ConfigType.QotdTimeMinuteUtc:
                    return reader.GetInt32(index);
                case ConfigType.SuggestionPingRoleId:
                    return reader.IsDBNull(index) ? null : reader.GetString(index);
                case ConfigType.SuggestionChannelId:
                    return reader.IsDBNull(index) ? null : reader.GetString(index);
                case ConfigType.LogsChannelId:
                    return reader.IsDBNull(index) ? null : reader.GetString(index);
            };

            throw new DatabaseException($"Unable to find parameter `{configType}` in configuration.");
        }

        #endregion

        #region Questions

        public enum QuestionType
        {
            Suggested = 0,
            Accepted = 1,
            Sent = 2,
        }

        public const string QuestionsTable = @"
                CREATE TABLE IF NOT EXISTS Question (
                    Id INTEGER PRIMARY KEY,
                    GuildId TEXT NOT NULL,
                    Type INTEGER NOT NULL, -- enum value of QuestionType
                    Text TEXT NOT NULL,
                    SubmittedByUserId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL
                );
            ";
        #endregion
    }
}
