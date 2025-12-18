using DSharpPlus.Commands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Configs.Commands
{
    public sealed partial class ConfigCommand
    {
        [Command("set")]
        public sealed class ConfigSetCommand
        {
            [Command("general")]
            [Description("Set config values related to general settings")]
            public static async Task SetGeneralAsync(CommandContext context,
                [Description("The display name of the profile this config belongs to (default \"QOTD\")")] string? ProfileName = null,
                [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
                [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole? AdminRole = null,
                [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel? NoticesLevel = null,
                [Description("Whether questions should get the \"Stashed\" type instead of being deleted (true by default).")] bool? EnableDeletedToStash = null,
                [Description("The channel where executed admin commands get logged to.")] DiscordChannel? LogsChannel = null
            )
            => await Helpers.General.SetAllAsync(context,
                ProfileName: ProfileName,
                BasicRole: BasicRole,
                AdminRole: AdminRole,
                NoticesLevel: NoticesLevel,
                EnableDeletedToStash: EnableDeletedToStash,
                LogsChannel: LogsChannel);

            [Command("qotd_sending")]
            [Description("Set config values related to QOTD sending")]
            public static async Task SetQotdSendingAsync(CommandContext context,
                [Description("The channel the QOTD should get sent in.")] DiscordChannel? Channel = null,
                [Description("The hour of the day the QOTDs should get sent in UTC time (0-23).")] int? TimeHourUtc = null,
                [Description("The minute of the day the QOTDs should get sent in UTC time (0-59).")] int? TimeMinuteUtc = null,
                [Description("Specifies on which days the QOTDs should get sent (sends daily if unset).")] string? TimeDayCondition = null,
                [Description("Specifies how a question should get altered after being sent as QOTD.")] Config.AlterQuestionAfterSentOption? AlterQuestionAfterSent = null,
                [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool? EnableAutomaticQotd = null,
                [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool? EnableAutomaticPresets = null,
                [Description("Whether to send a warning embed when the sent QOTD is the last available (true by default).")] bool? EnableLastAvailableWarn = null,
                [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool? EnableUnavailableMessage = null)
            => await Helpers.General.SetAllAsync(context,
                QotdChannel: Channel,
                QotdTimeHourUtc: TimeHourUtc,
                QotdTimeMinuteUtc: TimeMinuteUtc,
                QotdTimeDayCondition: TimeDayCondition,
                QotdAlterQuestionAfterSent: AlterQuestionAfterSent,
                EnableAutomaticQotd: EnableAutomaticQotd,
                EnableQotdAutomaticPresets: EnableAutomaticPresets,
                EnableQotdLastAvailableWarn: EnableLastAvailableWarn,
                EnableQotdUnavailableMessage: EnableUnavailableMessage);

            [Command("qotd_message")]
            [Description("Set config values related to QOTD message appearance and behavior")]
            public static async Task SetQotdMessageAsync(CommandContext context,
                [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? PingRole = null,
                [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] string? Title = null,
                [Description("The shorthand that is sometimes displayed in place of the title. (defaults to \"QOTD\") if unset)")] string? Shorthand = null,
                [Description("Hex color code of the QOTD embed message. (defaults to \"#8acfac\") if unset).")] string? EmbedColorHex = null,
                [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool? EnablePinMessage = null,
                [Description("Whether to automatically create a thread for every QOTD that gets sent (false by default).")] bool? EnableCreateThread = null,
                [Description("Whether to include a button for general info about OpenQOTD under sent QOTDs (true by default).")] bool? EnableShowInfoButton = null,
                [Description("Whether to include a footer with info and a questions left count in sent QOTDs (true by default).")] bool? EnableShowFooter = null,
                [Description("Whether to include the username of who suggested the QOTD (true by default).")] bool? EnableShowCredit = null,
                [Description("Whether to include a counter to QOTDs (eg. \"QOTD #42\", uses Sent count; true by default).")] bool? EnableShowCounter = null)
            => await Helpers.General.SetAllAsync(context,
                QotdPingRole: PingRole,
                QotdTitle: Title,
                QotdShorthand: Shorthand,
                QotdEmbedColorHex: EmbedColorHex,
                EnableQotdPinMessage: EnablePinMessage,
                EnableQotdCreateThread: EnableCreateThread,
                EnableQotdShowInfoButton: EnableShowInfoButton,
                EnableQotdShowFooter: EnableShowFooter,
                EnableQotdShowCredit: EnableShowCredit,
                EnableQotdShowCounter: EnableShowCounter);

            [Command("suggestions")]
            [Description("Set config values related to QOTD user suggestions")]
            public static async Task SetSuggestionsAsync(CommandContext context,
                [Description("Whether to allow users with the basic_role to suggest QOTDs (true by default).")] bool? Enabled = null,
                [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? Channel = null,
                [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? PingRole = null,
                [Description("Whether to pin suggestion messages when they are sent to the suggestions channel (true by default).")] bool? EnablePinMessage = null
            )
            => await Helpers.General.SetAllAsync(context, 
                EnableSuggestions: Enabled,
                SuggestionsChannel: Channel,
                SuggestionsPingRole: PingRole,
                EnableSuggestionsPinMessage: EnablePinMessage);
        }
    }
}
