using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.ComponentModel;
using OpenQotd.Bot.Helpers.Profiles;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using OpenQotd.Bot.EventHandlers.Suggestions;

namespace OpenQotd.Bot.Commands
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

            DiscordInteractionResponseBuilder modal = CreateSuggestionEventHandlers.GetQotdModal(config, context.Guild!.Name);

            await (context as SlashCommandContext)!.RespondWithModalAsync(modal);
        }


        [Command("qotd")]
        [Description("Suggest a Question Of The Day to be added. Unlike /suggest, this uses the default profile.")]
        public static async Task QotdAsync(CommandContext context)
        { 
            Config? defaultConfig = await ProfileHelpers.TryGetDefaultConfigAsync(context);
            if (defaultConfig is null)
                return;

            await SuggestAsync(context, defaultConfig.ProfileId);
        }
    }
}
