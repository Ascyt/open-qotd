using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using OpenQotd.Bot.Helpers.Profiles;

namespace OpenQotd.Bot.Commands
{
    [Command("presets")]
    public class PresetsCommand
    {
        public enum PresetsType
        {
            Active, Completed
        }

        [Command("list")]
        [Description("List all presets.")]
        public static async Task ListPresetsAsync(CommandContext context,
            [Description("Optionally filter by only active or completed presets.")] PresetsType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await CommandRequirements.UserIsAdmin(context, config))
                return;

            await PrintPresetDisabledWarningIfRequired(context, config);

            HashSet<PresetSent> presetSents;
            using (AppDbContext dbContext = new())
            {
                presetSents = [.. await dbContext.PresetSents
                        .Where(p => p.ConfigId == config.Id).ToListAsync()];
            }
            List<Presets.GuildDependentPreset> guildDependentPresets = Presets.GetPresetsAsGuildDependent(presetSents);

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNew(context, page, $"{(type != null ? $"{type} " : "")}Presets List",
                Task<PageInfo<Presets.GuildDependentPreset>> (int page) =>
                {
                    int totalPresets = guildDependentPresets.Count;

                    int totalPages = (int)Math.Ceiling(totalPresets / (double)itemsPerPage);

                    Presets.GuildDependentPreset[] presetsInPage = [.. guildDependentPresets
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    PageInfo<Presets.GuildDependentPreset> pageInfo = new()
                    {
                        Elements = presetsInPage,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalPresets,
                        TotalPagesCount = totalPages,
                    };

                    return Task.FromResult(pageInfo);
                }, ListPresetToString);
        }


        [Command("setactive")]
        [Description("Set whether a preset is enabled to be sent as QOTD or not.")]
        public static async Task SetPresetActiveAsync(CommandContext context,
            [Description("The ID of the preset.")] int id,
            [Description("Whether to set the preset as active to be sendable as QOTD.")] bool active)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await CommandRequirements.UserIsAdmin(context, config))
                return;

            await PrintPresetDisabledWarningIfRequired(context, config);

            if (id < 0 || id >= Presets.Values.Length)
            {
                await context.RespondAsync(GenericEmbeds.Error($"ID must be between 0 and {Presets.Values.Length - 1}."));
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

            string presetString = (new Presets.GuildDependentPreset(id, !active)).ToString();
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

        [Command("reset")]
        [Description("Reset the active state of all presets, making them all QOTD-sendable again.")]
        public static async Task ResetPresetsAsync(CommandContext context)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await CommandRequirements.UserIsAdmin(context, config))
                return;

            await PrintPresetDisabledWarningIfRequired(context, config);

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

        [Command("suggest")]
        [Description("Suggest a preset to be added globally to OpenQOTD!")]
        public static async Task SuggestPresetAsync(CommandContext context,
            [Description("The QOTD question text to be suggested.")] string question)
            => await SimpleCommands.FeedbackAsync(context, $"Preset Suggestion: {question}");

        /// <summary>
        /// Prints a warning message if automatic presets are disabled in the guild config.
        /// </summary>
        private static async Task PrintPresetDisabledWarningIfRequired(CommandContext context, Config config)
        {
            if (config.EnableQotdAutomaticPresets)
                return;

            await context.Channel.SendMessageAsync(
                GenericEmbeds.Warning("Presets are currently disabled and will not be automatically sent.\n\n" +
                "*They can be enabled with `/config set enable_qotd_automatic_presets True`.*")
                );
        }

        private static string ListPresetToString(Presets.GuildDependentPreset preset, int rank)
            => preset.ToString();
    }
}
