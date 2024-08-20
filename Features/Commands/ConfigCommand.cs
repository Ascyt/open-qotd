using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
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
            SuggestionPingRoleId,
            SuggestionChannelId,
        }

        [Command("set")]
        [System.ComponentModel.Description("Set a config value")]
        public static async Task SetAsync(CommandContext context,
            [System.ComponentModel.Description("The type of the config")] ConfigType type,
            [System.ComponentModel.Description("The value to set the config to")] string value)
        {
            await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed("Successfully set config", $"{type}, {value}")
                );
        }
    }
}
