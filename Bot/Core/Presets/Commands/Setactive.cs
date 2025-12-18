using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Presets.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Presets.Commands
{
    public sealed partial class PresetsCommand
    {
        [Command("setactive")]
        [Description("Set whether a preset is enabled to be sent as QOTD or not.")]
        public static async Task SetPresetActiveAsync(CommandContext context,
            [Description("The ID of the preset.")] int id,
            [Description("Whether to set the preset as active to be sendable as QOTD.")] bool active)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await Permissions.Api.Admin.UserIsAdmin(context, config))
                return;

            await Helpers.General.PrintPresetDisabledWarningIfRequired(context, config);

            if (id < 0 || id >= Api.Presets.Length)
            {
                await context.RespondAsync(GenericEmbeds.Error($"ID must be between 0 and {Api.Presets.Length - 1}."));
                return;
            }

            bool changesMade = false;

            using (AppDbContext dbContext = new())
            {
                PresetSent? preset = await dbContext.PresetSents.FirstOrDefaultAsync(p => p.ConfigId == config.Id && p.PresetIndex == id);

                if (preset != null) // Preset sent and disabled
                {
                    if (active)
                    {
                        dbContext.PresetSents.Remove(preset);
                        changesMade = true;
                    }
                }
                else // Preset enabled and active
                {
                    if (!active)
                    {
                        await dbContext.PresetSents.AddAsync(new PresetSent() { ConfigId = config.Id, PresetIndex = id });
                        changesMade = true;
                    }
                }

                if (changesMade)
                    await dbContext.SaveChangesAsync();
            }

            string presetString = new Api.GuildDependentPreset(id, !active).ToString();
            if (changesMade)
            {
                await context.RespondAsync(
                    GenericEmbeds.Success(title:"Preset Set", presetString)
                    );
            }
            else
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title:"No Changes Made", 
                    message:$"There are no changes that have been made to the preset:\n\n> {presetString}\n\n*The preset is already of the specified active type.*")
                    );
            }
        }
    }
}
