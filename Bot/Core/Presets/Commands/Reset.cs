using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Logging;
using OpenQotd.Core.Presets.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Presets.Commands
{
    public sealed partial class PresetsCommand
    {
        [Command("reset")]
        [Description("Reset the active state of all presets, making them all QOTD-sendable again.")]
        public static async Task ResetPresetsAsync(CommandContext context)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            await Helpers.General.PrintPresetDisabledWarningIfRequired(context, config);

            using (AppDbContext dbContext = new())
            {
                List<PresetSent> toRemove = await dbContext.PresetSents.Where(ps => ps.ConfigId == config.Id).ToListAsync();

                dbContext.RemoveRange(toRemove);

                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                GenericEmbeds.Success(title: "Presets Reset", "All presets have been resetted and are now sendable as QOTDs again.")
                );
        }
    }
}
