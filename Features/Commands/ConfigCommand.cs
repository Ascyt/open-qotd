using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using System.Text;
using System.Threading.Channels;
using static CustomQotd.Database.DatabaseValues;

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
            [System.ComponentModel.Description("The UTC hour of the day the QOTDs should get sent.")] int QotdTimeHourUtc,
            [System.ComponentModel.Description("The UTC minute of the day the QOTDs should get sent.")] int QotdTimeMinuteUtc,
            [System.ComponentModel.Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null)
        {
            try
            {
                if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("Administrator server permission is required to run this command.")
                        );
                }

                Dictionary<ConfigType, object?> values = new()
                {
                    { ConfigType.BasicRoleId, BasicRole?.Id },
                    { ConfigType.AdminRoleId, AdminRole.Id },
                    { ConfigType.QotdChannelId, QotdChannel.Id },
                    { ConfigType.QotdTimeHourUtc, QotdTimeHourUtc },
                    { ConfigType.QotdTimeMinuteUtc, QotdTimeMinuteUtc },
                    { ConfigType.QotdPingRoleId, QotdPingRole?.Id },
                    { ConfigType.SuggestionChannelId, SuggestionsChannel?.Id },
                    { ConfigType.SuggestionPingRoleId, SuggestionsPingRole?.Id },
                };

                await DatabaseApi.InitializeConfigAsync(context.Guild.Id, values);

                await context.RespondAsync(
                        MessageHelpers.GenericSuccessEmbed("Successfully initialized config", "View with `/config get`")
                    );
            }
            catch (DatabaseException ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
        }

        [Command("get")]
        [System.ComponentModel.Description("Get all config values")]
        public static async Task GetAsync(CommandContext context)
        {
            try
            {
                if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("Administrator server permission is required to run this command.")
                        );
                }

                Dictionary<ConfigType, object?> values = await DatabaseApi.GetConfigAsync(context.Guild.Id);

                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<ConfigType, object?> value in values)
                {
                    sb.AppendLine($"- **{value.Key}**: `{value.Value ?? "{unset}"}`");
                }

                await context.RespondAsync(
                        MessageHelpers.GenericEmbed($"Config values", $"{sb}")
                    );
            }
            catch (DatabaseException ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
        }

        [Command("set")]
        [System.ComponentModel.Description("Set a config value")]
        public static async Task SetAsync(CommandContext context,
            [System.ComponentModel.Description("The role a user needs to have to execute any basic commands (allows anyone by default).")] DiscordRole? BasicRole = null,
            [System.ComponentModel.Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole? AdminRole = null,
            [System.ComponentModel.Description("The channel the QOTD should get sent in.")] DiscordChannel? QotdChannel = null,
            [System.ComponentModel.Description("The UTC hour of the day the QOTDs should get sent.")] int? QotdTimeHourUtc = null,
            [System.ComponentModel.Description("The UTC minute of the day the QOTDs should get sent.")] int? QotdTimeMinuteUtc = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null)
        {
            try
            {
                if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("Administrator server permission is required to run this command.")
                        );
                }

                Dictionary<ConfigType, object?> values = new()
                {
                    { ConfigType.BasicRoleId, BasicRole?.Id },
                    { ConfigType.AdminRoleId, AdminRole?.Id },
                    { ConfigType.QotdChannelId, QotdChannel?.Id },
                    { ConfigType.QotdTimeHourUtc, QotdTimeHourUtc },
                    { ConfigType.QotdTimeMinuteUtc, QotdTimeMinuteUtc },
                    { ConfigType.QotdPingRoleId, QotdPingRole?.Id },
                    { ConfigType.SuggestionChannelId, SuggestionsChannel?.Id },
                    { ConfigType.SuggestionPingRoleId, SuggestionsPingRole?.Id },
                };

                if (values.Values.All(v => v is null))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("At least one argument must be specified.")
                        );
                    return;
                }

                StringBuilder valuesSet = new StringBuilder();

                foreach (KeyValuePair<ConfigType, object?> value in values)
                {
                    if (value.Value is not null)
                    {
                        await DatabaseApi.SetConfigAsync(context.Guild.Id, value.Key, value.Value);

                        valuesSet.AppendLine($"- **{value.Key}**: `{value.Value}`");
                    }
                }

                await context.RespondAsync(
                        MessageHelpers.GenericSuccessEmbed("Successfully set config", $"{valuesSet}")
                    );
            }
            catch (DatabaseException ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
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
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] SingleOption? SuggestionsPingRole = null)
        {

            try
            {
                Dictionary<ConfigType, bool> isSet = new()
                {
                    { ConfigType.BasicRoleId, BasicRole != null },
                    { ConfigType.QotdPingRoleId, QotdPingRole != null },
                    { ConfigType.SuggestionChannelId, SuggestionsChannel != null },
                    { ConfigType.SuggestionPingRoleId, SuggestionsPingRole != null },
                };


                if (isSet.Values.All(v => v == false))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("At least one argument must be specified.")
                        );
                    return;
                }

                StringBuilder valuesReset = new StringBuilder();

                foreach (KeyValuePair<ConfigType, bool> value in isSet)
                {
                    if (value.Value == true)
                    {
                        await DatabaseApi.SetConfigAsync(context.Guild.Id, value.Key, null);

                        if (!string.IsNullOrEmpty(valuesReset.ToString()))
                        {
                            valuesReset.Append(", ");
                        }
                        valuesReset.Append($"`{value.Key}`");
                    }
                }

                await context.RespondAsync(
                        MessageHelpers.GenericSuccessEmbed("Successfully resetted config values", $"{valuesReset}")
                    );
            }
            catch (DatabaseException ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
        }
    }
}
