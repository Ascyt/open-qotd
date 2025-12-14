using Microsoft.EntityFrameworkCore;
using OpenQotd.QotdSending;
using System.ComponentModel.DataAnnotations;

namespace OpenQotd.Database.Entities
{
    public class Config
    {
        /// <summary>
        /// Whether no, only important, or all notices should be sent to the guild under QOTDs.
        /// </summary>
        public enum NoticeLevel
        {
            /// <summary>
            /// No notices will be sent at all.
            /// </summary>
            None = 0, 
            /// <summary>
            /// Only important notices (e.g. maintenance) will be sent.
            /// </summary>
            Important = 1, 
            /// <summary>
            /// All notices (including maintenance, new features, etc.) will be sent.
            /// </summary>
            All = 2 
        }
        /// <summary>
        /// What to do with a question after it has been sent as a QOTD.
        /// </summary>
        public enum AlterQuestionAfterSentOption
        {
            /// <summary>
            /// The question gets the Sent type.
            /// </summary>
            QuestionToSent = 0,
            /// <summary>
            /// The question gets the Sent type, and if there are no more Accepted questions, all Sent questions are reset to Available.
            /// </summary>
            QuestionToSentAndResetIfEmpty = 1,
            /// <summary>
            /// The question remains Accepted and can thus be sent again in the future.
            /// </summary>
            QuestionStaysAccepted = 2,
            /// <summary>
            /// The question gets the Suggested type again.
            /// </summary>
            QuestionToSuggested = 3,
            /// <summary>
            /// The question gets the Stashed type if <see cref="EnableDeletedToStash"/> is true, otherwise it is permanently deleted.
            /// </summary>
            RemoveQuestion = 4
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
        /// If true, send a warning message to the QOTD channel if the sent question was the last available question.
        /// </summary>
        public bool EnableQotdLastAvailableWarn { get; set; } = true;

        /// <summary>
        /// If true, send a message to the QOTD channel if no question is available when a QOTD is to be sent.
        /// </summary>
        public bool EnableQotdUnavailableMessage { get; set; } = true;

        /// <summary>
        /// If true, show an info button for general info about the bot under the QOTD message.
        /// </summary>
        public bool EnableQotdShowInfoButton { get; set; } = true;

        /// <summary>
        /// If true, show the footer in the QOTD embed with info on how many questions are left and the question ID.
        /// </summary>
        public bool EnableQotdShowFooter { get; set; } = true;

        /// <summary>
        /// If true, show the username of the submittor in the QOTD embed.
        /// </summary>
        public bool EnableQotdShowCredit { get; set; } = true;

        /// <summary>
        /// If true, show a counter of the sent index this QOTD is (e.g. "QOTD #42").
        /// </summary>
        public bool EnableQotdShowCounter { get; set; } = true;

        /// <summary>
        /// The hour (0-23) in UTC when the daily QOTD is sent if automatic QOTDs are enabled.
        /// </summary>
        public int QotdTimeHourUtc { get; set; }

        /// <summary>
        /// The minute (0-59) in UTC when the daily QOTD is sent if automatic QOTDs are enabled.
        /// </summary>
        public int QotdTimeMinuteUtc { get; set; }

        /// <summary>
        /// Optional condition for sending the QOTD on specific days only, eg. every two weeks. 
        /// </summary>
        /// <remarks>
        /// If null or empty, the QOTD is sent every day.
        /// </remarks>
        [MaxLength(16)]
        public string? QotdTimeDayCondition { get; set; } = null;

        /// <summary>
        /// What to do with a question after it has been sent as a QOTD.
        /// </summary>
        public AlterQuestionAfterSentOption QotdAlterQuestionAfterSent { get; set; } = AlterQuestionAfterSentOption.QuestionToSent;

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
        /// The hex color code of the QOTD embed. If null, the default color is used.
        /// </summary>
        public string? QotdEmbedColorHex { get; set; } = null;
        public string QotdEmbedColorHexEffective => QotdEmbedColorHex ?? Program.AppSettings.ConfigQotdEmbedColorHexDefault;

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
        /// If true, the bot will pin suggestion messages when they are sent to the suggestions channel.
        /// </summary>
        /// <remarks>
        /// If this is true, any accepted/denied suggestion messages will also be unpinned.
        /// </remarks>
        public bool EnableSuggestionsPinMessage { get; set; } = true;

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

        // 
        // -------------------------------- Internal variables (not viewable and not set using /config) --------------------------------
        //

        /// <summary>
        /// The timestamp when this config/profile was initialized. 
        /// </summary>
        public DateTime InitializedTimestamp { get; set; }

        /// <summary>
        /// When the last QOTD was sent. Used to prevent multiple QOTDs being sent in a single day.
        /// </summary>
        public DateTime? LastSentTimestamp { get; set; }

        /// <summary>
        /// The timestamp when <see cref="QotdTimeDayCondition"/> was last set or changed. 
        /// </summary>
        /// <remarks>
        /// Used to calculate whether the condition is met for the "every n days/weeks/..."-condition.
        /// </remarks>
        public DateTime? QotdTimeDayConditionLastChangedTimestamp { get; set; }

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
                $"**General:**\n" +
                $"- basic_role: {FormatRole(BasicRoleId)}\n" +
                $"- admin_role: {FormatRole(AdminRoleId)}\n" +
                $"- notices_level: **{NoticesLevel}**\n" +
                $"- enable_deleted_questions_to_stash: **{EnableDeletedToStash}**\n" +
                $"- logs_channel: {FormatChannel(LogsChannelId)}\n" +
                $"\n" +
                $"**QOTD Sending:**\n" +
                $"- channel: {FormatChannel(QotdChannelId)}\n" +
                $"- time_hour_utc: **{QotdTimeHourUtc}**\n" +
                $"- time_minute_utc: **{QotdTimeMinuteUtc}**\n" +
                $"- time_day_condition: {(QotdTimeDayCondition is null ? "*daily*" : $"`{QotdTimeDayCondition}`")}\n" +
                $"- alter_question_after_sent: **{QotdAlterQuestionAfterSent}**\n" +
                $"- enable_automatic_qotd: **{EnableAutomaticQotd}**\n" +
                $"- enable_automatic_presets: **{EnableQotdAutomaticPresets}**\n" +
                $"- enable_last_available_warn: **{EnableQotdLastAvailableWarn}**\n" +
                $"- enable_unavailable_message: **{EnableQotdUnavailableMessage}**\n" +
                $"\n" +
                $"**QOTD Message:**\n" +
                $"- ping_role: {FormatRole(QotdPingRoleId)}\n" +
                $"- title: *{QotdTitleText}*{(QotdTitle is null ? " (default)" : "")}\n" +
                $"- shorthand: *{QotdShorthandText}*{(QotdShorthand is null ? " (default)" : "")}\n" +
                $"- embed_color_hex: `{QotdEmbedColorHexEffective}`{(QotdEmbedColorHex is null ? " (default)" : "")}\n" +
                $"- enable_pin_message: **{EnableQotdPinMessage}**\n" +
                $"- enable_create_thread: **{EnableQotdCreateThread}**\n" +
                $"- enable_show_info_button: **{EnableQotdShowInfoButton}**\n" +
                $"- enable_show_footer: **{EnableQotdShowFooter}**\n" +
                $"- enable_show_credit: **{EnableQotdShowCredit}**\n" +
                $"- enable_show_counter: **{EnableQotdShowCounter}**\n" +
                $"\n" +
                $"**Suggestions:**\n" +
                $"- enabled: **{EnableSuggestions}**\n" +
                $"- channel: {FormatChannel(SuggestionsChannelId)}\n" +
                $"- ping_role: {FormatRole(SuggestionsPingRoleId)}\n" +
                $"- enable_pin_message: **{EnableSuggestionsPinMessage}**";
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
