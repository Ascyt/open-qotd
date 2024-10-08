using CustomQotd.Database.Entities;
using DSharpPlus.Commands;
using System.ComponentModel;

namespace CustomQotd.Features.Commands
{
    [Command("presets")]
    public class PresetsCommand
    {
        private enum PresetsType
        {
            Enabled, Disabled
        }

        [Command("list")]
        [Description("List all presets.")]
        public static async Task ListPresetsAsync(CommandContext context,
            [Description("Optionally filter by only enabled or disabled presets.")] PresetsType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        { 
        }
    }
}
