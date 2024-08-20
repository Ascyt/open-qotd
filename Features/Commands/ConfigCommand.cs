using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Channels;

namespace CustomQotd.Features.Commands
{
    [Command("config")]
    public class ConfigCommand
    {
        public enum ConfigType
        {
            AdminRoleId,
            QotdChannelId,
            QotdPingRoleId,
            QotdTimeHourUtc,
            QotdTimeMinuteUtc,
            SuggestionChannelId,
            SuggestionPingRoleId,
        }
        // TODO: More config options: 
        // SendIfEmpty (bool), BasicRoleId (nullable, AdminRoleId overrides this), exclude/include days of week
        // Plus the ability to costumize the QOTD (premium feature)

        [Command("initialize")]
        [System.ComponentModel.Description("Initialize the config with values")]
        public static async Task InitializeAsync(CommandContext context,
            [System.ComponentModel.Description("The role a user needs to have to execute admin commands.")] DiscordRole AdminRole,
            [System.ComponentModel.Description("The channel the QOTD should get sent in.")] DiscordChannel QotdChannel,
            [System.ComponentModel.Description("The UTC hour of the day the QOTDs should get sent.")] int QotdTimeHourUtc,
            [System.ComponentModel.Description("The UTC minute of the day the QOTDs should get sent.")] int QotdTimeMinuteUtc,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is sent.")] DiscordRole? QotdPingRole = null,
            [System.ComponentModel.Description("The channel new QOTD suggestions get announced in.")] DiscordChannel? SuggestionsChannel = null,
            [System.ComponentModel.Description("The role that will get pinged when a new QOTD is suggested.")] DiscordRole? SuggestionsPingRole = null)
        {
            try
            {
                if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("Must have Administrator server permission to run this command")
                        );
                }

                Dictionary<ConfigType, object?> values = new()
                {
                    { ConfigType.AdminRoleId, AdminRole.Id },
                    { ConfigType.QotdChannelId, QotdChannel.Id },
                    { ConfigType.QotdTimeHourUtc, QotdTimeHourUtc },
                    { ConfigType.QotdTimeMinuteUtc, QotdTimeMinuteUtc },
                    { ConfigType.QotdPingRoleId, QotdPingRole?.Id },
                    { ConfigType.SuggestionChannelId, SuggestionsChannel?.Id },
                    { ConfigType.SuggestionPingRoleId, SuggestionsPingRole?.Id },
                };

                await DatabaseHelper.InitializeConfigAsync(context.Guild.Id, values);

                await context.RespondAsync(
                        MessageHelpers.GenericSuccessEmbed("Successfully initialized config", "View with `/config get`")
                    );
            }
            catch (DatabaseHelper.Exception ex)
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
                        MessageHelpers.GenericErrorEmbed("Must have Administrator server permission to run this command")
                        );
                }

                Dictionary<ConfigType, object?> values = await DatabaseHelper.GetConfigAsync(context.Guild.Id);

                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<ConfigType, object?> value in values)
                {
                    sb.AppendLine($"- **{value.Key}**: `{value.Value ?? "{unset}"}`");
                }

                await context.RespondAsync(
                        MessageHelpers.GenericEmbed($"Config values", $"{sb}")
                    );
            }
            catch (DatabaseHelper.Exception ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
        }

        [Command("set")]
        [System.ComponentModel.Description("Set a config value")]
        public static async Task SetAsync(CommandContext context,
            [System.ComponentModel.Description("The type of the config")] ConfigType type,
            [System.ComponentModel.Description("The value to set the config to")] string? value = null)
        {
            try
            {
                if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed("Must have Administrator server permission to run this command")
                        );
                }

                if (value == "null" || value == "" || value == "unset" || value == "{unset}")
                    value = null;

                await DatabaseHelper.SetConfigAsync(context.Guild.Id, type, value);

                await context.RespondAsync(
                        MessageHelpers.GenericSuccessEmbed("Successfully set config", $"**{type}**: `{value ?? "{unset}"}`")
                    );
            }
            catch (DatabaseHelper.Exception ex)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(ex.Message)
                    );
            }
        }
    }
}
