using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace OpenQotd.Bot.Database.Entities
{
    public class Config
    {
        public enum NoticeLevel
        {
            None = 0,
            Important = 1,
            All = 2
        }

        public int Id { get; set; }
        public ulong GuildId { get; set; }

        public ulong? BasicRoleId { get; set; }
        public ulong AdminRoleId { get; set; }

        public ulong QotdChannelId { get; set; }
        public ulong? QotdPingRoleId { get; set; }
        public bool EnableAutomaticQotd { get; set; } = true;
        public bool EnableQotdPinMessage { get; set; } = true;
        public bool EnableQotdCreateThread { get; set; } = false;
        public bool EnableQotdAutomaticPresets { get; set; } = true;
        public bool EnableQotdUnavailableMessage { get; set; } = true;

        public int QotdTimeHourUtc { get; set; }
        public int QotdTimeMinuteUtc { get; set; }
        public bool EnableSuggestions { get; set; } = true;
        public ulong? SuggestionsChannelId { get; set; }
        public ulong? SuggestionsPingRoleId { get; set; }
        public NoticeLevel NoticesLevel { get; set; } = NoticeLevel.All;
        public ulong? LogsChannelId { get; set; }
        public bool EnableDeletedToStash { get; set; } = true;

        // Variables (not set using /config)
        public DateTime? LastSentTimestamp { get; set; }

        public int? CurrentSuggestStreak { get; set; }
        public ulong? CurrentSuggestStreakUserId { get; set; }

        public ulong? LastQotdMessageId { get; set; }

        public override string ToString()
        {
            return
                $"- basic_role: {FormatRole(BasicRoleId)}\n" +
                $"- admin_role: {FormatRole(AdminRoleId)}\n" +
                $"- qotd_channel: {FormatChannel(QotdChannelId)}\n" +
                $"- qotd_ping_role: {FormatRole(QotdPingRoleId)}\n" +
                $"- enable_automatic_qotd: **{EnableAutomaticQotd}**\n" +
                $"- enable_qotd_pin_message: **{EnableQotdPinMessage}**\n" +
                $"- enable_qotd_create_thread: **{EnableQotdCreateThread}**\n" +
                $"- enable_qotd_automatic_presets: **{EnableQotdAutomaticPresets}**\n" +
                $"- enable_qotd_unavailable_message: **{EnableQotdUnavailableMessage}**\n" +
                $"- qotd_time_hour_utc: **{QotdTimeHourUtc}**\n" +
                $"- qotd_time_minute_utc: **{QotdTimeMinuteUtc}**\n" +
                $"- enable_suggestions: **{EnableSuggestions}**\n" +
                $"- suggestions_channel: {FormatChannel(SuggestionsChannelId)}\n" +
                $"- suggestions_ping_role: {FormatRole(SuggestionsPingRoleId)}\n" +
                $"- notices_level: **{NoticesLevel}**\n" +
                $"- enable_deleted_questions_to_stash: **{EnableDeletedToStash}**\n" +
                $"- logs_channel: {FormatChannel(LogsChannelId)}";
        }

        private static string FormatRole(ulong? roleId)
        {
            return roleId is null ? "*unset*" : $"<@&{roleId}>";
        }
        private static string FormatChannel(ulong? channelId)
        {
            return channelId is null ? "*unset*" : $"<#{channelId}>";
        }
    }
}
