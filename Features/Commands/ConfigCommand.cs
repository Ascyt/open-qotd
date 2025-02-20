using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using System.ComponentModel;
using System.Text;
using System.Threading.Channels;
using static CustomQotd.Features.Logging;

namespace CustomQotd.Features.Commands
{
    [Command("config")]
    public class ConfigCommand
    {
        // TODO: More config options: 
        // SendIfEmpty (bool), BasicRoleId (nullable, AdminRoleId overrides this), exclude/include days of week
        // Plus the ability to costumize the QOTD (premium feature)

        [Command("initialize")]
        [Description("Initialize the config with values")]
        public static async Task InitializeAsync(CommandContext context,
            [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole AdminRole,
            [Description("The channel the QOTD should get sent in.")] DiscordChannel QotdChannel,
            [Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int QotdTimeHourUtc,
            [Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int QotdTimeMinuteUtc,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool EnableAutomaticQotd = true,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool EnableQotdPinMessage = true,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool EnableQotdAutomaticPresets = true,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool EnableQotdUnvailableMessage = true,
            [Description("Whether to allow users with the BasicRole to suggest QOTDs (true by default).")] bool EnableSuggestions = true,
            [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel NoticesLevel = Config.NoticeLevel.All,
            [Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!context.Member!.Permissions.HasPermission(DiscordPermissions.Administrator))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed("Server Administrator permission is required to run this command.")
                    );
            }

            QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc, 0, 59);
            QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc, 0, 23);

            Config config = new Config
            {
                GuildId = context!.Guild!.Id,
                BasicRoleId = BasicRole?.Id,
                AdminRoleId = AdminRole.Id,
                QotdChannelId = QotdChannel.Id,
                QotdPingRoleId = QotdPingRole?.Id,
                EnableAutomaticQotd = EnableAutomaticQotd,
                EnableQotdPinMessage = EnableQotdPinMessage,
                EnableQotdAutomaticPresets = EnableQotdAutomaticPresets,
                EnableQotdUnavailableMessage = EnableQotdUnvailableMessage,
                QotdTimeHourUtc = QotdTimeHourUtc,
                QotdTimeMinuteUtc = QotdTimeMinuteUtc,
                EnableSuggestions = EnableSuggestions,
                SuggestionsChannelId = SuggestionsChannel?.Id,
                SuggestionsPingRoleId = SuggestionsPingRole?.Id,
                NoticesLevel = NoticesLevel,
                LogsChannelId = LogsChannel?.Id
            };
            using (var dbContext = new AppDbContext())
            {
                Config? existingConfig = await dbContext.Configs.FindAsync(context!.Guild.Id);
                if (existingConfig != null)
                {
                    existingConfig = config;
                }
                else
                {
                    await dbContext.Configs.AddAsync(config);
                }
                await dbContext.SaveChangesAsync();
            }
            string configString = await config.ToStringAsync();

            await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed("Successfully initialized config", configString)
                );

            // Can cause issues
            // await LogUserAction(context, "Initialize config", configString);
        }

        [Command("get")]
        [Description("Get all config values")]
        public static async Task GetAsync(CommandContext context)
        {
            if (!context.Member!.Permissions.HasPermission(DiscordPermissions.Administrator))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed("Server Administrator permission is required to run this command.")
                    );
            }

            if (!await CommandRequirements.IsConfigInitialized(context))
                return;

            Config config;
            using (var dbContext = new AppDbContext())
            {
                config = (await dbContext.Configs.Where(c => c.GuildId == context.Guild!.Id).FirstOrDefaultAsync())!;
            }

            string configString = await config.ToStringAsync();


            await context.RespondAsync(
                    MessageHelpers.GenericEmbed($"Config values", $"{configString}")
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
            [Description("Whether to send a QOTD daily automatically, if disabled `/trigger` is needed (true by default).")] bool? EnableAutomaticQotd = null,
            [Description("Whether to pin the most recent QOTD to the channel or not (true by default).")] bool? EnableQotdPinMessage = null,
            [Description("Whether to send a random preset when there is no Accepted QOTD available (true by default).")] bool? EnableQotdAutomaticPresets = null,
            [Description("Whether to send a \"not available\" message when there is no QOTD available (true by default).")] bool? EnableQotdUnavailableMessage = null,
            [Description("Whether to allow users with the BasicRole to suggest QOTDs (true by default).")] bool? EnableSuggestions = null,
            [Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [Description("Whether all, only important, or no notices should be shown under QOTDs (all by default).")] Config.NoticeLevel? NoticesLevel = null,
            [Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!context.Member!.Permissions.HasPermission(DiscordPermissions.Administrator))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed("Server Administrator permission is required to run this command.")
                    );
            }

            if (!await CommandRequirements.IsConfigInitialized(context))
                return;
            if (QotdTimeMinuteUtc is not null)
                QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc.Value, 0, 59);
            if (QotdTimeHourUtc is not null)
                QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc.Value, 0, 23);

            Config config;
            using (var dbContext = new AppDbContext())
            {
                config = dbContext.Configs.Where(c => c.GuildId == context.Guild!.Id).FirstOrDefault()!;

                if ((QotdTimeMinuteUtc is not null || QotdTimeHourUtc is not null))
                {
                    int currentDay = DateTime.UtcNow.Day;

                    if (config.LastSentTimestamp?.Day == currentDay)
                    {
                        await context.Channel!.SendMessageAsync(MessageHelpers.GenericWarningEmbed("Since a QOTD has already been sent today, the next one will be sent tomorrow at the specified time.\n\n" +
                            "*Use `/trigger` to send a QOTD anyways!*"));
                    }
                }

                if (BasicRole is not null) 
                    config.BasicRoleId = BasicRole.Id;
                if (AdminRole is not null)
                    config.AdminRoleId = AdminRole.Id;
                if (QotdChannel is not null)
                    config.QotdChannelId = QotdChannel.Id;
                if (EnableAutomaticQotd is not null)
                    config.EnableAutomaticQotd = EnableAutomaticQotd.Value;
                if (EnableQotdPinMessage is not null)
                    config.EnableQotdPinMessage = EnableQotdPinMessage.Value;
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
                if (LogsChannel is not null)
                    config.LogsChannelId = LogsChannel.Id;

                await dbContext.SaveChangesAsync();   
            }

            string configString = await config.ToStringAsync();

            await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed("Successfully set config values", $"{configString}")
                );

            await LogUserAction(context, "Set config values", configString);
        }

        public enum SingleOption
        {
            Reset
        }

        [Command("reset")]
        [Description("Reset optional config values to be unset")]
        public static async Task ResetAsync(CommandContext context,
            [Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] SingleOption? BasicRole = null,
            [Description("The role that will get pinged when a new QOTD is sent.")] SingleOption? QotdPingRole = null,
            [Description("The channel new QOTD suggestions get announced in.")] SingleOption? SuggestionsChannel = null,
            [Description("The role that will get pinged when a new QOTD is suggested.")] SingleOption? SuggestionsPingRole = null,
            [Description("The channel where commands, QOTDs and more get logged to.")] SingleOption? LogsChannel = null)
        {
            Config config;
            using (var dbContext = new AppDbContext())
            {
                config = dbContext.Configs.Where(c => c.GuildId == context.Guild!.Id).FirstOrDefault()!;

                if (BasicRole is not null)
                    config.BasicRoleId = null;
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

            string configString = await config.ToStringAsync();

            await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed("Successfully set config values", $"{configString}")
                );

            await LogUserAction(context, "Set config values", configString);
        }
    }
}
