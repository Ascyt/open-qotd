using DSharpPlus.Commands;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Presets.Commands.Helpers
{
    internal static class General
    {
        /// <summary>
        /// Prints a warning message if automatic presets are disabled in the guild config.
        /// </summary>
        public static async Task PrintPresetDisabledWarningIfRequired(CommandContext context, Config config)
        {
            if (config.EnableQotdAutomaticPresets)
                return;

            await context.Channel.SendMessageAsync(
                GenericEmbeds.Warning("Presets are currently disabled and will not be automatically sent.\n\n" +
                "*They can be enabled with `/config set enable_qotd_automatic_presets True`.*")
                );
        }
    }
}
