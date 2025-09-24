using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;
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
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] string? QotdTitle = null,
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool EnableAutomaticQotd = true,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool EnableQotdPinMessage = true,
            [Description("Whether to automatically create a thread for every QOTD that gets sent (false by default).")] bool EnableQotdCreateThread = false,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool EnableQotdAutomaticPresets = true,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool EnableQotdUnvailableMessage = true,
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

            if (QotdTitle is not null && !await IsQotdTitleValid(context, QotdTitle))
                return;

            int? profileId = await ProfileHelpers.GetSelectedProfileIdAsync(context.Guild!.Id, context.Member!.Id);

            int profileIdNotNull = profileId ?? 0;
            Config config = new()
            {
                GuildId = context!.Guild!.Id,
                ProfileId = profileIdNotNull,
                IsDefaultProfile = profileId is null,
                ProfileName = ProfileHelpers.GenerateProfileName(profileId),
                BasicRoleId = BasicRole?.Id,
                AdminRoleId = AdminRole.Id,
                QotdChannelId = QotdChannel.Id,
                QotdPingRoleId = QotdPingRole?.Id,
                QotdTitle = QotdTitle,
                EnableAutomaticQotd = EnableAutomaticQotd,
                EnableQotdPinMessage = EnableQotdPinMessage,
                EnableQotdCreateThread = EnableQotdCreateThread,
                EnableQotdAutomaticPresets = EnableQotdAutomaticPresets,
                EnableQotdUnavailableMessage = EnableQotdUnvailableMessage,
                QotdTimeHourUtc = QotdTimeHourUtc,
                QotdTimeMinuteUtc = QotdTimeMinuteUtc,
                EnableSuggestions = EnableSuggestions,
                SuggestionsChannelId = SuggestionsChannel?.Id,
                SuggestionsPingRoleId = SuggestionsPingRole?.Id,
                NoticesLevel = NoticesLevel,
                EnableDeletedToStash = EnableDeletedToStash,
                LogsChannelId = LogsChannel?.Id
            };
            bool reInitialized = false;

            using (AppDbContext dbContext = new())
            {
                Config? existingConfig = await dbContext.Configs
                    .FirstOrDefaultAsync(c => c.GuildId == context.Guild.Id && c.ProfileId == profileIdNotNull);

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

            await context.RespondAsync(
                    GenericEmbeds.Success($"Successfully {(reInitialized ? "re-" : "")}initialized config", configString, profileName:config.ProfileName)
                );

            // Can cause issues
            // await LogUserAction(context, "Initialize config", configString);
        }

        [Command("get")]
        [Description("Get all config values")]
        public static async Task GetAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedConfigAsync(context);

            if (config is null)
                return;

            string configString = config.ToString();

            await context.RespondAsync(
                    GenericEmbeds.Custom($"Config values", $"{configString}")
                );
        }

        [Command("set")]
        [Description("Set a config value")]
        public static async Task SetAsync(CommandContext context,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole? AdminRole = null,
            [Description("The channel the QOTD should get sent in.")] DiscordChannel? QotdChannel = null,
            [Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int? QotdTimeHourUtc = null,
            [Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int? QotdTimeMinuteUtc = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] string? QotdTitle = null,
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool? EnableAutomaticQotd = null,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool? EnableQotdPinMessage = null,
            [Description("Whether to automatically create a thread for every QOTD that gets sent (false by default).")] bool? EnableQotdCreateThread = null,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool? EnableQotdAutomaticPresets = null,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool? EnableQotdUnavailableMessage = null,
            [Description("Whether to allow users with the BasicRole to suggest QOTDs (true by default).")] bool? EnableSuggestions = null,
            [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel? NoticesLevel = null,
			[Description("Whether questions should get the \"Stashed\" type instead of being deleted (true by default).")] bool? EnableDeletedToStash = null,
			[Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedConfigAsync(context);

            if (config is null)
                return;

            if (QotdTimeMinuteUtc is not null)
                QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc.Value, 0, 59);
            if (QotdTimeHourUtc is not null)
                QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc.Value, 0, 23);

            if (QotdTitle is not null && !await IsQotdTitleValid(context, QotdTitle))
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
                        await context.Channel!.SendMessageAsync(GenericEmbeds.Warning("Since a QOTD has already been sent today, the next one will be sent tomorrow at the specified time.\n\n" +
                            "*Use `/trigger` to send a QOTD anyways!*"));
                    }
                }

                if (BasicRole is not null) 
                    config.BasicRoleId = BasicRole.Id;
                if (AdminRole is not null)
                    config.AdminRoleId = AdminRole.Id;
                if (QotdChannel is not null)
                    config.QotdChannelId = QotdChannel.Id;
                if (QotdTitle is not null)
                    config.QotdTitle = QotdTitle;
                if (EnableAutomaticQotd is not null)
                    config.EnableAutomaticQotd = EnableAutomaticQotd.Value;
                if (EnableQotdPinMessage is not null)
                    config.EnableQotdPinMessage = EnableQotdPinMessage.Value;
                if (EnableQotdCreateThread is not null)
                    config.EnableQotdCreateThread = EnableQotdCreateThread.Value;
                if (EnableQotdAutomaticPresets is not null)
                    config.EnableQotdAutomaticPresets = EnableQotdAutomaticPresets.Value;
                if (EnableQotdUnavailableMessage is not null)
                    config.EnableQotdUnavailableMessage = EnableQotdUnavailableMessage.Value;
                if (QotdTimeHourUtc is not null)
                    config.QotdTimeHourUtc = QotdTimeHourUtc.Value;
                if (QotdTimeMinuteUtc is not null)
                    config.QotdTimeMinuteUtc = QotdTimeMinuteUtc.Value;
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

            string configString = config.ToString();

            await context.RespondAsync(
                    GenericEmbeds.Success("Successfully set config values", $"{configString}")
                );

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
            [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\") if unset)")] SingleOption? QotdTitle = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] SingleOption? QotdPingRole = null,
            [Description("The channel new QOTD suggestions get announced in.")] SingleOption? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] SingleOption? SuggestionsPingRole = null,
            [Description("The channel where commands, QOTDs and more get logged to.")] SingleOption? LogsChannel = null)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedConfigAsync(context);

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

            string configString = config.ToString();

            await context.RespondAsync(
                    GenericEmbeds.Success("Successfully set config values", $"{configString}")
                );

            await LogUserAction(context, config, "Set config values", message: configString);
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
    }
}
