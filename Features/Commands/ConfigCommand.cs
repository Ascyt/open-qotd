using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SQLitePCL;
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
        [System.ComponentModel.Description("Initialize the config with values")]
        public static async Task InitializeAsync(CommandContext context,
            [System.ComponentModel.Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole AdminRole,
            [System.ComponentModel.Description("The channel the QOTD should get sent in.")] DiscordChannel QotdChannel,
            [System.ComponentModel.Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int QotdTimeHourUtc,
            [System.ComponentModel.Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int QotdTimeMinuteUtc,
            [System.ComponentModel.Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [System.ComponentModel.Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed("Server Administrator permission is required to run this command.")
                    );
            }

            Config config = new Config
            {
                GuildId = context.Guild.Id,
                BasicRoleId = BasicRole?.Id,
                AdminRoleId = AdminRole.Id,
                QotdChannelId = QotdChannel.Id,
                QotdPingRoleId = QotdPingRole?.Id,
                QotdTimeHourUtc = QotdTimeHourUtc,
                QotdTimeMinuteUtc = QotdTimeMinuteUtc,
                SuggestionsChannelId = SuggestionsChannel?.Id,
                SuggestionsPingRoleId = SuggestionsPingRole?.Id,
                LogsChannelId = LogsChannel?.Id
            };
            using (var dbContext = new AppDbContext())
            {
                await dbContext.Configs.AddAsync(config);
                await dbContext.SaveChangesAsync();
            }
            string configString = await config.ToStringAsync();

            await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed("Successfully initialized config", configString)
                );

            await LogUserAction(context, "Initialize config", configString);
        }

        [Command("get")]
        [System.ComponentModel.Description("Get all config values")]
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
        [System.ComponentModel.Description("Set a config value")]
        public static async Task SetAsync(CommandContext context,
            [System.ComponentModel.Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [System.ComponentModel.Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole? AdminRole = null,
            [System.ComponentModel.Description("The channel the QOTD should get sent in.")] DiscordChannel? QotdChannel = null,
            [System.ComponentModel.Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int? QotdTimeHourUtc = null,
            [System.ComponentModel.Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int? QotdTimeMinuteUtc = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null,
            [System.ComponentModel.Description("The channel where commands, QOTDs and more get logged to.")] DiscordChannel? LogsChannel = null)
        {
            if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
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
                config = dbContext.Configs.Where(c => c.GuildId == context.Guild!.Id).FirstOrDefault()!;

                if (BasicRole is not null) 
                    config.BasicRoleId = BasicRole.Id;
                if (AdminRole is not null)
                    config.AdminRoleId = AdminRole.Id;
                if (QotdChannel is not null)
                    config.QotdChannelId = QotdChannel.Id;
                if (QotdTimeHourUtc is not null)
                    config.QotdTimeHourUtc = QotdTimeHourUtc.Value;
                if (QotdTimeMinuteUtc is not null)
                    config.QotdTimeMinuteUtc = QotdTimeMinuteUtc.Value;
                if (QotdPingRole is not null)
                    config.QotdPingRoleId = QotdPingRole.Id;
                if (SuggestionsChannel is not null)
                    config.SuggestionsChannelId = SuggestionsChannel.Id;
                if (SuggestionsPingRole is not null)
                    config.SuggestionsPingRoleId = SuggestionsPingRole.Id;
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
        [System.ComponentModel.Description("Reset optional config values to be unset")]
        public static async Task ResetAsync(CommandContext context,
            [System.ComponentModel.Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] SingleOption? BasicRole = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] SingleOption? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] SingleOption? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] SingleOption? SuggestionsPingRole = null,
            [System.ComponentModel.Description("The channel where commands, QOTDs and more get logged to.")] SingleOption? LogsChannel = null)
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
