using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OpenQotd.Bot.Database.Entities
{
    public class Config
    {
        /// <summary>
        /// Whether no, only important, or all notices should be sent to the guild under QOTDs.
        /// </summary>
        public enum NoticeLevel
        {
            None = 0,
            Important = 1,
            All = 2
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        /// <summary>
        /// Guild-dependent ID of the profile this config belongs to. 
        /// </summary>
        /// <remarks>
        /// One profile is unique per config, but a guild can have multiple profiles.
        /// </remarks>
        public int ProfileId { get; set; }

        /// <summary>
        /// Whether this is the default profile for the guild. Each guild can only have one default profile.
        /// </summary>
        public bool IsDefaultProfile { get; set; } = false;

        /// <summary>
        /// User-defined name for this config profile.
        /// </summary>
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Users without this role cannot use basic commands like suggesting questions or viewing the leaderboard. If null, everyone can use basic commands.
        /// </summary>
        public ulong? BasicRoleId { get; set; }

        /// <summary>
        /// Users without this role cannot use administrative commands like manually adding questions or triggering the next QOTD.
        /// </summary>
        /// <remarks>
        /// For /config commands, only users with the Administrator permission can use them, regardless of this setting.
        /// </remarks>
        public ulong AdminRoleId { get; set; }

        /// <summary>
        /// The channel where QOTDs are sent.
        /// </summary>
        public ulong QotdChannelId { get; set; }

        /// <summary>
        /// The role to ping when a new QOTD is sent. If null, no role is pinged.
        /// </summary>
        public ulong? QotdPingRoleId { get; set; }

        /// <summary>
        /// Daily QOTDs are sent automatically at the configured time if this is true.
        /// </summary>
        /// <remarks>
        /// If this is false, QOTDs can still be sent manually using /trigger.
        /// </remarks>
        public bool EnableAutomaticQotd { get; set; } = true;

        /// <summary>
        /// If true, the bot will pin the new QOTD message and unpin the old one when it is sent.
        /// </summary>
        public bool EnableQotdPinMessage { get; set; } = true;

        /// <summary>
        /// If true, the bot will create a thread for the new QOTD message when it is sent.
        /// </summary>
        public bool EnableQotdCreateThread { get; set; } = false;

        /// <summary>
        /// If true, the bot will automatically use preset questions when there are no user-submitted questions available.
        /// </summary>
        /// <remarks>
        /// User-submitted questions are always preferred over presets if available.
        /// </remarks>
        public bool EnableQotdAutomaticPresets { get; set; } = true;

        /// <summary>
        /// If true, send a message to the QOTD channel if no question is available when a QOTD is to be sent.
        /// </summary>
        public bool EnableQotdUnavailableMessage { get; set; } = true;

        /// <summary>
        /// If true, show an info button for general info about the bot under the QOTD message.
        /// </summary>
        public bool EnableQotdShowInfoButton { get; set; } = true;

        /// <summary>
        /// The hour (0-23) in UTC when the daily QOTD is sent if automatic QOTDs are enabled.
        /// </summary>
        public int QotdTimeHourUtc { get; set; }

        /// <summary>
        /// The minute (0-59) in UTC when the daily QOTD is sent if automatic QOTDs are enabled.
        /// </summary>
        public int QotdTimeMinuteUtc { get; set; }

        /// <summary>
        /// The title of the QOTD message. If null the default is used which is "Question Of The Day"
        /// </summary>
        public string? QotdTitle { get; set; } = null;
        public string QotdTitleText => QotdTitle ?? Program.AppSettings.ConfigQotdTitleDefault;

        /// <summary>
        /// The shorthand of the QOTD title. If null the default is used which is "QOTD"
        /// </summary>
        public string? QotdShorthand { get; set; } = null;
        public string QotdShorthandText => QotdShorthand ?? Program.AppSettings.ConfigQotdShorthandDefault;

        /// <summary>
        /// If true, users can suggest questions using /suggest or /qotd.
        /// </summary>
        public bool EnableSuggestions { get; set; } = true;

        /// <summary>
        /// Specifies the channel where suggestion notifications are sent to. If null, suggestions are not sent anywhere.
        /// </summary>
        public ulong? SuggestionsChannelId { get; set; }

        /// <summary>
        /// Specifies the role to ping when a new suggestion is made. If null, no role is pinged.
        /// </summary>
        /// <remarks>
        /// A warning will be sent if this is set but <see cref="SuggestionsChannelId"/> is null.
        /// </remarks>
        public ulong? SuggestionsPingRoleId { get; set; }

        /// <summary>
        /// Which notices should be sent to the guild under QOTDs.
        /// </summary>
        public NoticeLevel NoticesLevel { get; set; } = NoticeLevel.All;

        /// <summary>
        /// Specifies the channel where logs about changes to questions, config or similar are sent to. If null, no logs are sent.
        /// </summary>
        public ulong? LogsChannelId { get; set; }

        /// <summary>
        /// If true, questions that are deleted are moved to the stash instead of being permanently deleted.
        /// </summary>
        /// <remarks>
        /// A question that is already of type <see cref="QuestionType.Stashed"/> will also be permanently deleted.
        /// </remarks>
        public bool EnableDeletedToStash { get; set; } = true;

        // Internal variables (not viewable and not set using /config)

        /// <summary>
        /// When the last QOTD was sent. Used to prevent multiple QOTDs being sent in a single day.
        /// </summary>
        public DateTime? LastSentTimestamp { get; set; }

        /// <summary>
        /// The ID of the last QOTD message sent. 
        /// </summary>
        /// <remarks>
        /// Used for unpinning the old message if <see cref="EnableQotdPinMessage"/> is true.
        /// </remarks>
        public ulong? LastQotdMessageId { get; set; }
        
        public override string ToString()
        {
            return
                $"**Profile:** *{ProfileName}*{(IsDefaultProfile ? " (default profile)" : "")}\n" +
                $"\n" +
                $"- basic_role: {FormatRole(BasicRoleId)}\n" +
                $"- admin_role: {FormatRole(AdminRoleId)}\n" +
                $"- qotd_channel: {FormatChannel(QotdChannelId)}\n" +
                $"- qotd_ping_role: {FormatRole(QotdPingRoleId)}\n" +
                $"- qotd_title: *{QotdTitle ?? Program.AppSettings.ConfigQotdTitleDefault}*\n" +
                $"- qotd_shorthand: *{QotdShorthand ?? Program.AppSettings.ConfigQotdShorthandDefault}*\n" +
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

        /// <summary>
        /// Generates the next available ProfileId for a new question in the specified config.
        /// </summary>
        /// <returns>The next ID, or null if default config is not initialized yet.</returns>
        public static async Task<int?> TryGetNextProfileId(ulong guildId)
        {
            using AppDbContext dbContext = new();
            try
            {
                int maxExistentProfile = await dbContext.Configs
                    .Where(q => q.GuildId == guildId)
                    .Select(q => q.ProfileId)
                    .MaxAsync();

                int maxSelectedProfile;
                try
                {
                    maxSelectedProfile = await dbContext.ProfileSelections
                        .Where(gu => gu.GuildId == guildId)
                        .Select(gu => gu.SelectedProfileId)
                        .MaxAsync();
                }
                catch (InvalidOperationException)
                {
                    maxSelectedProfile = -1; // Nobody has a selected profile yet
                }

                return Math.Max(maxExistentProfile, maxSelectedProfile) + 1;
            }
            catch (InvalidOperationException)
            {
                return null; // No configs yet for this guild
            }
        }
    }
}
