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
        [Command("list")]
        [Description("List all presets.")]
        public static async Task ListPresetsAsync(CommandContext context,
            [Description("Optionally filter by only active or completed presets.")] PresetsType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await Permissions.Api.Admin.UserIsAdmin(context, config))
                return;

            await Helpers.General.PrintPresetDisabledWarningIfRequired(context, config);

            HashSet<PresetSent> presetSents;
            using (AppDbContext dbContext = new())
            {
                presetSents = [.. await dbContext.PresetSents
                        .Where(p => p.ConfigId == config.Id).ToListAsync()];
            }
            List<Api.GuildDependentPreset> guildDependentPresets = Api.GetPresetsAsGuildDependent(presetSents);

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNew(context, page, $"{(type != null ? $"{type} " : "")}Presets List",
                Task<PageInfo<Api.GuildDependentPreset>> (int page) =>
                {
                    int totalPresets = guildDependentPresets.Count;

                    Api.GuildDependentPreset[] presetsInPage = [.. guildDependentPresets
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    PageInfo<Api.GuildDependentPreset> pageInfo = new()
                    {
                        Elements = presetsInPage,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalPresets
                    };

                    return Task.FromResult(pageInfo);
                }, ListPresetToString);
        }

        private static string ListPresetToString(Api.GuildDependentPreset preset, int rank)
            => preset.ToString();
    }
}
