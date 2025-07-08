using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CustomQotd.Features.Commands
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
            if (!await CommandRequirements.UserIsAdmin(context))
                return;

            await PrintPresetDisabledWarningIfRequired(context);

            const int itemsPerPage = 10;
            await MessageHelpers.ListMessageComplete(context, page, $"{(type != null ? $"{type} " : "")}Presets List", 
                async Task<(Presets.PresetBySent[], int, int, int)> (int page) =>
            {
                HashSet<PresetSent> presetSents;

                using (var dbContext = new AppDbContext())
                {
                    presetSents = (await dbContext.PresetSents
                        .Where(p => p.GuildId == context.Guild!.Id).ToListAsync()).ToHashSet();
                }

                List<Presets.PresetBySent> presetsBySent = Presets.GetValuesBySent(presetSents);

                int totalPresets = presetsBySent.Count;

                int totalPages = (int)Math.Ceiling(totalPresets / (double)itemsPerPage);

                Presets.PresetBySent[] presetsInPage = presetsBySent
                    .Skip((page - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToArray();

                return (presetsInPage, totalPresets, totalPages, itemsPerPage);
            });
        }


        [Command("setactive")]
        [Description("Set whether a preset is enabled to be sent as QOTD or not.")]
        public static async Task SetPresetActiveAsync(CommandContext context,
            [Description("The ID of the preset.")] int id,
            [Description("Whether to set the preset as active to be sendable as QOTD.")] bool active)
        {
            if (!await CommandRequirements.UserIsAdmin(context))
                return;

            await PrintPresetDisabledWarningIfRequired(context);

            if (id < 0 || id >= Presets.Values.Length)
            {
                await context.RespondAsync(MessageHelpers.GenericErrorEmbed($"ID must be between 0 and {Presets.Values.Length - 1}."));
                return;
            }

            bool changesMade = false;

            using (var dbContext = new AppDbContext())
            {
                PresetSent? preset = await dbContext.PresetSents.FirstOrDefaultAsync(p => p.GuildId == context.Guild!.Id && p.PresetIndex == id);

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
                        await dbContext.PresetSents.AddAsync(new PresetSent() { GuildId = context.Guild!.Id, PresetIndex = id });
                        changesMade = true;
                    }
                }

                if (changesMade)
                    await dbContext.SaveChangesAsync();
            }

            string presetString = (new Presets.PresetBySent(id, !active)).ToString();
            if (changesMade)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericSuccessEmbed(title:"Preset Set", presetString)
                    );
            }
            else
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title:"No Changes Made", 
                    message:$"There are no changes that have been made to the preset:\n\n> {presetString}\n\n*The preset is already of the specified active type.*")
                    );
            }
        }

        [Command("reset")]
        [Description("Reset the active state of all presets, making them all QOTD-sendable again.")]
        public static async Task ResetPresetsAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserIsAdmin(context))
                return;

            await PrintPresetDisabledWarningIfRequired(context);

            using (var dbContext = new AppDbContext())
            {
                List<PresetSent> toRemove = await dbContext.PresetSents.Where(ps => ps.GuildId == context.Guild!.Id).ToListAsync();

                dbContext.RemoveRange(toRemove);

                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed(title: "Presets Reset", "All presets have been resetted and are now sendable as QOTDs again.")
                );
        }

        [Command("suggest")]
        [Description("Suggest a preset to be added globally to OpenQOTD!")]
        public static async Task SuggestPresetAsync(CommandContext context,
            [Description("The QOTD question text to be suggested.")] string question)
            => await SimpleCommands.FeedbackAsync(context, $"Preset Suggestion: {question}");

        private static async Task PrintPresetDisabledWarningIfRequired(CommandContext context)
        {
            bool enableQotdAutomaticPresets;
            using (var dbContext = new AppDbContext())
            {
                enableQotdAutomaticPresets = dbContext.Configs
                    .Where(c => c.GuildId == context.Guild!.Id)
                    .Select(c => c.EnableQotdAutomaticPresets)
                    .First();
            }

            if (enableQotdAutomaticPresets)
                return;

            await context.Channel.SendMessageAsync(
                MessageHelpers.GenericWarningEmbed("Presets are currently disabled and will not be automatically sent.\n\n" +
                "*They can be enabled with `/config set enable_qotd_automatic_presets True`.*")
                );
        }
    }
}
