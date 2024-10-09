using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CustomQotd.Database.Entities
{
    public class Config
    {
        [Key]
        public ulong GuildId { get; set; }

        public ulong? BasicRoleId { get; set; }
        public ulong AdminRoleId { get; set; }

        public ulong QotdChannelId { get; set; }
        public ulong? QotdPingRoleId { get; set; }
        public bool EnableAutomaticQotd { get; set; } = true;
        public bool EnableQotdPinMessage { get; set; } = true;
        public bool EnableQotdAutomaticPresets { get; set; } = true;
        public bool EnableQotdUnavailableMessage { get; set; } = true;

        public int QotdTimeHourUtc { get; set; }
        public int QotdTimeMinuteUtc { get; set; }
        public bool EnableSuggestions { get; set; } = true;
        public ulong? SuggestionsChannelId { get; set; }
        public ulong? SuggestionsPingRoleId { get; set; }
        public ulong? LogsChannelId { get; set; }

        // Variables (not set using /config)
        public int? LastSentDay { get; set; }

        public int? CurrentSuggestStreak { get; set; }
        public ulong? CurrentSuggestStreakUserId { get; set; }

        public ulong? LastQotdMessageId { get; set; }

        /// <summary>
        /// Do not use ToString(), use ToStringAsync() instead
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override string ToString()
        {
            throw new NotImplementedException($"Use ToStringAsync() instead");
        }

        public async Task<string> ToStringAsync()
        {
            DiscordGuild guild = await Program.Client.GetGuildAsync(GuildId);

            return
                $"- basic_role: {await RoleIdToString(BasicRoleId, guild)}\n" +
                $"- admin_role: {await RoleIdToString(AdminRoleId, guild)}\n" +
                $"- qotd_channel: {await ChannelIdToString(QotdChannelId, guild)}\n" +
                $"- qotd_ping_role: {await RoleIdToString(QotdPingRoleId, guild)}\n" +
                $"- enable_automatic_qotd: **{EnableAutomaticQotd}**\n" +
                $"- enable_qotd_pin_message: **{EnableQotdPinMessage}**\n" +
                $"- enable_qotd_automatic_presets: **{EnableQotdAutomaticPresets}**\n" +
                $"- enable_qotd_unavailable_message: **{EnableQotdUnavailableMessage}**\n" +
                $"- qotd_time_hour_utc: **{QotdTimeHourUtc}**\n" +
                $"- qotd_time_minute_utc: **{QotdTimeMinuteUtc}**\n" +
                $"- enable_suggestions: **{EnableSuggestions}**\n" + 
                $"- suggestions_channel: {await ChannelIdToString(SuggestionsChannelId, guild)}\n" +
                $"- suggestions_ping_role: {await RoleIdToString(SuggestionsPingRoleId, guild)}\n" +
                $"- logs_channel: {await ChannelIdToString(LogsChannelId, guild)}";
        }

        private static async Task<string> RoleIdToString(ulong? roleId, DiscordGuild guild)
        {
            try
            {
                if (roleId == null)
                    return "*unset*";

                DiscordRole role = await guild.GetRoleAsync(roleId.Value);

                return $"{role.Mention} (`{roleId}`)";
            }
            catch (NotFoundException)
            {
                return $"`{roleId}` *(role not found)*";
            }
        }

        private static async Task<string> ChannelIdToString(ulong? channelId, DiscordGuild guild)
        {
            try
            {
                if (channelId == null)
                    return "*unset*";

                DiscordChannel channel = await guild.GetChannelAsync(channelId.Value);

                return $"{channel.Mention} (`{channelId}`)";
            }
            catch (NotFoundException)
            {
                return $"`{channelId}` *(channel not found)*";
            }
        }
    }
}
