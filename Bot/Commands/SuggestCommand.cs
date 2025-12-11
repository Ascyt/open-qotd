using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.ComponentModel;
using OpenQotd.Helpers.Profiles;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using OpenQotd.EventHandlers.Suggestions;

namespace OpenQotd.Commands
{
    public class SuggestCommand
    {
        [Command("suggest")]
        [Description("Suggest a Question Of The Day to be added. Opens up a modal to enter the question and details.")]
        public static async Task SuggestAsync(CommandContext context,
            [Description("Which OpenQOTD profile your question should be suggested to.")][SlashAutoCompleteProvider<SuggestableProfilesAutoCompleteProvider>] int For)
        {
            int profileId = For;

            Config? config = await ProfileHelpers.TryGetConfigAsync(context, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(context, config))
                return;

            if (!await CommandRequirements.IsWithinMaxQuestionsAmount(context, 1))
                return;

            if (!config.EnableSuggestions)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Suggestions Disabled", message: "Suggesting of QOTDs for this profile using `/qotd` or `/suggest` has been disabled by staff."));
                return;
            }

            DiscordModalBuilder modal = CreateSuggestionEventHandlers.GetQotdModal(config, context.Guild!.Name);

            await (context as SlashCommandContext)!.RespondWithModalAsync(modal);
        }


        [Command("qotd")]
        [Description("Suggest a Question Of The Day to be added. Unlike /suggest, this uses the selected/default profile.")]
        public static async Task QotdAsync(CommandContext context)
        { 
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            await SuggestAsync(context, config.ProfileId);
        }
    }
}
