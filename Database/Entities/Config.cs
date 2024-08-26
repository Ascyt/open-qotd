using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace CustomQotd.Database.Entities
{
    public class Config()
    {
        [Key]
        public ulong GuildId { get; set; }

        public ulong? BasicRoleId { get; set; }
        public ulong AdminRoleId { get; set; }

        public ulong QotdChannelId { get; set; }
        public ulong? QotdPingRoleId { get; set; }

        public int QotdTimeHourUtc { get; set; }
        public int QotdTimeMinuteUtc { get; set; }

        public ulong? SuggestionsChannelId { get; set; }
        public ulong? SuggestionsPingRoleId { get; set; }
        public ulong? LogsChannelId { get; set; }

        public override string ToString()
        {
            throw new NotImplementedException($"Use ToStringAsync() instead");
        }

        public async Task<string> ToStringAsync()
        {
            DiscordGuild guild = await Program.Client.GetGuildAsync(GuildId);

            return
                $"- basic_role: {RoleIdToString(BasicRoleId, guild)}\n" +
                $"- admin_role: {RoleIdToString(AdminRoleId, guild)}\n" +
                $"- qotd_channel: {ChannelIdToString(QotdChannelId, guild)}\n" +
                $"- qotd_ping_role: {RoleIdToString(QotdPingRoleId, guild)}\n" +
                $"- qotd_time_hour_utc: **{QotdTimeHourUtc}**\n" +
                $"- qotd_time_minute_utc: **{QotdTimeMinuteUtc}**\n" +
                $"- suggestions_channel: {ChannelIdToString(SuggestionsChannelId, guild)}\n" +
                $"- suggestions_ping_role: {RoleIdToString(SuggestionsPingRoleId, guild)}\n" +
                $"- logs_channel: {ChannelIdToString(LogsChannelId, guild)}";
        }

        private static async Task<string> RoleIdToString(ulong? roleId, DiscordGuild guild)
        {
            try
            {
                if (roleId == null)
                    return "`unset`";

                return $"**{(await guild.GetRoleAsync(roleId.Value)).Name}** (`{roleId}`)";
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
                    return "`unset`";

                return (await guild.GetChannelAsync(channelId.Value)).Mention;
            }
            catch (NotFoundException)
            {
                return $"`{channelId}` *(channel not found)*";
            }
        }
    }
}
