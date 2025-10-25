using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;
using OpenQotd.Bot.QotdSending;
using System.ComponentModel;
using System.Threading.Tasks;
using static OpenQotd.Bot.Logging;

namespace OpenQotd.Bot.Commands
{
    [Command("config")]
    public class ConfigCommand
    {
        [Command("initialize")]
        [Description("Initialize the config with values")]
        public static async Task InitializeAsync(CommandContext context,
            [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole AdminRole,
            [Description("The channel the QOTD should get sent in.")] DiscordChannel QotdChannel,
            [Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int QotdTimeHourUtc,
            [Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int QotdTimeMinuteUtc,
            [Description("Specifies on which days the QOTDs should get sent (sends daily if unset).")] string? QotdTimeDayCondition = null,
            [Description("The display name of the profile this config belongs to (default \"QOTD\")")] string? ProfileName = null,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] string? QotdTitle = null,
            [Description("The shorthand that is sometimes displayed in place of the title. (defaults to \"QOTD\") if unset)")] string? QotdShorthand = null,
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool EnableAutomaticQotd = true,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool EnableQotdPinMessage = true,
            [Description("Whether to automatically create a thread for every QOTD that gets sent (false by default).")] bool EnableQotdCreateThread = false,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool EnableQotdAutomaticPresets = true,
            [Description("Whether to send a warning embed when the sent QOTD is the last available (true by default).")] bool EnableQotdLastAvailableWarn    = true,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool EnableQotdUnvailableMessage = true,
            [Description("Whether to include a button for general info about OpenQOTD under sent QOTDs (true by default).")] bool EnableQotdShowInfoButton = true,
            [Description("Whether to allow users with the BasicRole to suggest QOTDs (true by default).")] bool EnableSuggestions = true,
            [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel NoticesLevel = Config.NoticeLevel.All,
			[Description("Whether questions should get the \"Stashed\" type instead of being deleted (true by default).")] bool EnableDeletedToStash = true,
			[Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc, 0, 59);
            QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc, 0, 23);

            if (QotdTimeDayCondition is not null && !await IsValidDayCondition(context, QotdTimeDayCondition))
                return;

            if (ProfileName is not null && !await IsProfileNameValid(context, ProfileName))
                return;
            if (QotdTitle is not null && !await IsQotdTitleValid(context, QotdTitle))
                return;
            if (QotdShorthand is not null && !await IsQotdShorthandValid(context, QotdShorthand))
                return;

            int existingConfigsCount;
            using (AppDbContext dbContext = new())
            {
                existingConfigsCount = await dbContext.Configs
                    .CountAsync(c => c.GuildId == context.Guild!.Id);
            }

            int profileId = await ProfileHelpers.GetSelectedOrDefaultProfileIdAsync(context.Guild!.Id, context.Member!.Id);

            Config config = new()
            {
                GuildId = context!.Guild!.Id,
                ProfileId = profileId,
                IsDefaultProfile = existingConfigsCount == 0,
                ProfileName = ProfileName ?? ProfileHelpers.GenerateProfileName(profileId),
                BasicRoleId = BasicRole?.Id,
                AdminRoleId = AdminRole.Id,
                QotdChannelId = QotdChannel.Id,
                QotdPingRoleId = QotdPingRole?.Id,
                QotdTitle = QotdTitle,
                QotdShorthand = QotdShorthand,
                EnableAutomaticQotd = EnableAutomaticQotd,
                EnableQotdPinMessage = EnableQotdPinMessage,
                EnableQotdCreateThread = EnableQotdCreateThread,
                EnableQotdAutomaticPresets = EnableQotdAutomaticPresets,
                EnableQotdLastAvailableWarn = EnableQotdLastAvailableWarn,
                EnableQotdUnavailableMessage = EnableQotdUnvailableMessage,
                EnableQotdShowInfoButton = EnableQotdShowInfoButton,
                QotdTimeHourUtc = QotdTimeHourUtc,
                QotdTimeMinuteUtc = QotdTimeMinuteUtc,
                QotdTimeDayCondition = QotdTimeDayCondition,
                QotdTimeDayConditionLastChangedTimestamp = QotdTimeDayCondition is null ? null : DateTime.UtcNow,
                EnableSuggestions = EnableSuggestions,
                SuggestionsChannelId = SuggestionsChannel?.Id,
                SuggestionsPingRoleId = SuggestionsPingRole?.Id,
                NoticesLevel = NoticesLevel,
                EnableDeletedToStash = EnableDeletedToStash,
                LogsChannelId = LogsChannel?.Id,
                InitializedTimestamp = DateTime.UtcNow
            };
            bool reInitialized = false;

            using (AppDbContext dbContext = new())
            {
                Config? existingConfig = await dbContext.Configs
                    .FirstOrDefaultAsync(c => c.GuildId == context.Guild.Id && c.ProfileId == profileId);

                if (existingConfig != null)
                {
                    dbContext.Remove(existingConfig);
                    await dbContext.SaveChangesAsync();
                    reInitialized = true;
                }

                await dbContext.Configs.AddAsync(config);
                await dbContext.SaveChangesAsync();
            }
            string configString = config.ToString();

            QotdSenderTimer.ConfigIdsToRecache.Add(config.Id);

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(
                    GenericEmbeds.Success($"Successfully {(reInitialized ? "re-" : "")}initialized config", configString, profileName: config.ProfileName)
                    );
            AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            // Can cause issues
            // await LogUserAction(context, "Initialize config", configString);
        }

        [Command("get")]
        [Description("Get all config values")]
        public static async Task GetAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Info(title: $"Config values", message: $"{configString}"));
            AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);
        }

        [Command("set")]
        [Description("Set a config value")]
        public static async Task SetAsync(CommandContext context, 
            [Description("The display name of the profile this config belongs to (default \"QOTD\")")] string? ProfileName = null,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole? AdminRole = null,
            [Description("The channel the QOTD should get sent in.")] DiscordChannel? QotdChannel = null,
            [Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int? QotdTimeHourUtc = null,
            [Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int? QotdTimeMinuteUtc = null,
            [Description("Specifies on which days the QOTDs should get sent (sends daily if unset).")] string? QotdTimeDayCondition = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] string? QotdTitle = null,
            [Description("The shorthand that is sometimes displayed in place of the title. (defaults to \"QOTD\") if unset)")] string? QotdShorthand = null,
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool? EnableAutomaticQotd = null,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool? EnableQotdPinMessage = null,
            [Description("Whether to automatically create a thread for every QOTD that gets sent (false by default).")] bool? EnableQotdCreateThread = null,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool? EnableQotdAutomaticPresets = null,
            [Description("Whether to send a warning embed when the sent QOTD is the last available (true by default).")] bool? EnableQotdLastAvailableWarn = null,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool? EnableQotdUnavailableMessage = null,
            [Description("Whether to include a button for general info about OpenQOTD under sent QOTDs (true by default).")] bool? EnableQotdShowInfoButton = null,
            [Description("Whether to allow users with the BasicRole to suggest QOTDs (true by default).")] bool? EnableSuggestions = null,
            [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel? NoticesLevel = null,
			[Description("Whether questions should get the \"Stashed\" type instead of being deleted (true by default).")] bool? EnableDeletedToStash = null,
			[Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);

            if (config is null)
                return;

            if (QotdTimeMinuteUtc is not null)
                QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc.Value, 0, 59);
            if (QotdTimeHourUtc is not null)
                QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc.Value, 0, 23);

            if (QotdTimeDayCondition is not null && !await IsValidDayCondition(context, QotdTimeDayCondition))
                return;

            if (ProfileName is not null && !await IsProfileNameValid(context, ProfileName))
                return;
            if (QotdTitle is not null && !await IsQotdTitleValid(context, QotdTitle))
                return;
            if (QotdShorthand is not null && !await IsQotdShorthandValid(context, QotdShorthand))
                return;

            using (AppDbContext dbContext = new())
            {
                // Without extra retrieval config changes don't get saved
                config = await dbContext.Configs
                    .FindAsync(config.Id);

                if (config is null)
                    throw new Exception("Config not found");

                if (QotdTimeMinuteUtc is not null || QotdTimeHourUtc is not null)
                {
                    int currentDay = DateTime.UtcNow.Day;

                    if (config.LastSentTimestamp?.Day == currentDay)
                    {
                        await context.Channel!.SendMessageAsync(GenericEmbeds.Warning($"Since a {config.QotdShorthandText} has already been sent today, the next one will be sent tomorrow at the specified time.\n\n" +
                            $"*Use `/trigger` to send a {config.QotdShorthandText} anyways!*"));
                    }
                }

                if (ProfileName is not null)
                    config.ProfileName = ProfileName;
                if (BasicRole is not null) 
                    config.BasicRoleId = BasicRole.Id;
                if (AdminRole is not null)
                    config.AdminRoleId = AdminRole.Id;
                if (QotdChannel is not null)
                    config.QotdChannelId = QotdChannel.Id;
                if (QotdTitle is not null)
                    config.QotdTitle = QotdTitle;
                if (QotdShorthand is not null)
                    config.QotdShorthand = QotdShorthand;
                if (EnableAutomaticQotd is not null)
                    config.EnableAutomaticQotd = EnableAutomaticQotd.Value;
                if (EnableQotdPinMessage is not null)
                    config.EnableQotdPinMessage = EnableQotdPinMessage.Value;
                if (EnableQotdCreateThread is not null)
                    config.EnableQotdCreateThread = EnableQotdCreateThread.Value;
                if (EnableQotdAutomaticPresets is not null)
                    config.EnableQotdAutomaticPresets = EnableQotdAutomaticPresets.Value;
                if (EnableQotdLastAvailableWarn is not null)
                    config.EnableQotdLastAvailableWarn = EnableQotdLastAvailableWarn.Value;
                if (EnableQotdUnavailableMessage is not null)
                    config.EnableQotdUnavailableMessage = EnableQotdUnavailableMessage.Value;
                if (EnableQotdShowInfoButton is not null)
                    config.EnableQotdShowInfoButton = EnableQotdShowInfoButton.Value;
                if (QotdTimeHourUtc is not null)
                    config.QotdTimeHourUtc = QotdTimeHourUtc.Value;
                if (QotdTimeMinuteUtc is not null)
                    config.QotdTimeMinuteUtc = QotdTimeMinuteUtc.Value;
                if (QotdTimeDayCondition is not null)
                {
                    config.QotdTimeDayCondition = QotdTimeDayCondition;
                    config.QotdTimeDayConditionLastChangedTimestamp = DateTime.UtcNow;
                }
                if (QotdPingRole is not null)
                    config.QotdPingRoleId = QotdPingRole.Id; 
                if (EnableSuggestions is not null)
                    config.EnableSuggestions = EnableSuggestions.Value;
                if (SuggestionsChannel is not null)
                    config.SuggestionsChannelId = SuggestionsChannel.Id;
                if (SuggestionsPingRole is not null)
                    config.SuggestionsPingRoleId = SuggestionsPingRole.Id;
                if (NoticesLevel is not null)
                    config.NoticesLevel = NoticesLevel.Value;
                if (EnableDeletedToStash is not null)
					config.EnableDeletedToStash = EnableDeletedToStash.Value;
				if (LogsChannel is not null)
                    config.LogsChannelId = LogsChannel.Id;

                await dbContext.SaveChangesAsync();   
            }

            QotdSenderTimer.ConfigIdsToRecache.Add(config.Id);

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Success("Successfully set config values", $"{configString}"));
            AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            await LogUserAction(context, config, "Set config values", message: configString);
        }

        public enum SingleOption
        {
            Reset
        }

        [Command("reset")]
        [Description("Reset optional config values to be unset")]
        public static async Task ResetAsync(CommandContext context,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] SingleOption? BasicRole = null,
            [Description("Specifies on which days the QOTDs should get sent (sends daily if unset).")] SingleOption? QotdTimeDayCondition = null,
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] SingleOption? QotdTitle = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] SingleOption? QotdPingRole = null,
            [Description("The channel new QOTD suggestions get announced in.")] SingleOption? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] SingleOption? SuggestionsPingRole = null,
            [Description("The channel where commands, QOTDs and more get logged to.")] SingleOption? LogsChannel = null)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);

            if (config is null)
                return;

            using (AppDbContext dbContext = new())
            {
                // Without extra retrieval config changes don't get saved
                config = await dbContext.Configs
                    .FindAsync(config.Id);

                if (config is null)
                    throw new Exception("Config not found");

                if (BasicRole is not null)
                    config.BasicRoleId = null;
                if (QotdTimeDayCondition is not null)
                {
                    config.QotdTimeDayCondition = null;
                    config.QotdTimeDayConditionLastChangedTimestamp = null;
                }
                if (QotdTitle is not null)  
                    config.QotdTitle = null;
                if (QotdPingRole is not null)
                    config.QotdPingRoleId = null;
                if (SuggestionsChannel is not null)
                    config.SuggestionsChannelId = null;
                if (SuggestionsPingRole is not null)
                    config.SuggestionsPingRoleId = null;
                if (LogsChannel is not null)
                    config.LogsChannelId = null;

                await dbContext.SaveChangesAsync();
            }

            QotdSenderTimer.ConfigIdsToRecache.Add(config.Id);

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Success("Successfully set config values", $"{configString}"));
            AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            await LogUserAction(context, config, "Set config values", message: configString);
        }

        private static void AddInfoButton(DiscordMessageBuilder builder, int profileId)
        {
            builder.AddActionRowComponent(
                new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"show-general-info-no-prompt/{profileId}",
                    "🛈"
                    )
                );
        }

        /// <summary>
        /// Checks whether or not the <paramref name="qotdTitle"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        private static async Task<bool> IsQotdTitleValid(CommandContext context, string qotdTitle)
        {
            if (qotdTitle.Length > Program.AppSettings.ConfigQotdTitleMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided QOTD Title must not exceed {Program.AppSettings.ConfigQotdTitleMaxLength} characters in length (provided length is {qotdTitle.Length}).")
                    );
                return false;
            }

            if (qotdTitle.Contains('\n'))
            {
                await context.RespondAsync($"The provided QOTD title must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether or not the <paramref name="qotdShorthand"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        private static async Task<bool> IsQotdShorthandValid(CommandContext context, string qotdShorthand)
        {
            if (qotdShorthand.Length > Program.AppSettings.ConfigQotdShorthandMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided QOTD shorthand must not exceed {Program.AppSettings.ConfigQotdShorthandMaxLength} characters in length (provided length is {qotdShorthand.Length}).")
                    );
                return false;
            }

            if (qotdShorthand.Contains('\n'))
            {
                await context.RespondAsync($"The provided QOTD shorthand must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether or not the <paramref name="profileName"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        private static async Task<bool> IsProfileNameValid(CommandContext context, string profileName)
        {
            if (profileName.Length > Program.AppSettings.ConfigProfileNameMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided profile name must not exceed {Program.AppSettings.ConfigProfileNameMaxLength} characters in length (provided length is {profileName.Length}).")
                    );
                return false;
            }

            if (profileName.Contains('\n'))
            {
                await context.RespondAsync($"The provided profile name must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        private static async Task<bool> IsValidDayCondition(CommandContext context, string dayCondition)
        {
            if (dayCondition.Length > 64)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided day condition must not exceed 64 characters in length (provided length is {dayCondition.Length}).")
                    );
                return false;
            }

            if (!IsValidDayConditionFormat(dayCondition))
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided day condition (`{dayCondition}`) is invalid. Please refer to the documentation for valid formats.")
                    );
                return false;
            }
            return true;
        }

        private static bool IsValidDayConditionFormat(string dayCondition)
        {
            if (dayCondition.Length < 2)
                return false;

            if (!dayCondition.StartsWith('%'))
                return false;
            
            switch (dayCondition[1])
            {
                case 'D': // Every 'n' days
                    if (dayCondition.Length < 3 || !int.TryParse(dayCondition[2..], out int n) || n < 1 || n > 31)
                        return false;
                    return true;
                case 'w': // Days of the week, starting with Monday=1
                    if (dayCondition.Length < 3)
                        return false;

                    string[] parts = dayCondition[2..].Split(',');
                    foreach (string part in parts)
                    {
                        if (!int.TryParse(part, out int day) || day < 1 || day > 7)
                            return false;
                    }

                    return true;
                case 'W': // Every nth week on the mth day of the week
                    if (dayCondition.Length < 5)
                        return false;
                    string[] parts1 = dayCondition[2..].Split(';');

                    if (parts1.Length != 2)
                        return false;

                    string weekIndexPart = parts1[0];
                    if (!int.TryParse(weekIndexPart, out int weekIndex) || weekIndex < 1)
                        return false;

                    string dayOfWeekPart = parts1[1];
                    if (!int.TryParse(dayOfWeekPart, out int dayOfWeek) || dayOfWeek < 1 || dayOfWeek > 7)
                        return false;
                    return true;
                case 'm': // Days of the month
                    if (dayCondition.Length < 3)
                        return false;

                    string[] parts2 = dayCondition[2..].Split(',');
                    foreach (string part in parts2)
                    {
                        if (!int.TryParse(part, out int day) || day < 1 || day > 31)
                            return false;
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
