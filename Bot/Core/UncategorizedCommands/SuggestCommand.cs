using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.UncategorizedCommands
{
    public class SuggestCommand
    {
        [Command("suggest")]
        [Description("Suggest a Question Of The Day to be added. Opens up a modal to enter the question and details.")]
        public static async Task SuggestAsync(CommandContext context,
            [Description("Which OpenQOTD profile your question should be suggested to.")][SlashAutoCompleteProvider<Profiles.AutoCompleteProviders.SuggestableProfiles>] int For)
        {
            int profileId = For;

            Config? config = await Profiles.Api.TryGetConfigAsync(context, profileId);
            if (config is null || !await Permissions.Api.Basic.UserIsBasic(context, config))
                return;

            if (!await Questions.Api.IsWithinMaxQuestionsAmount(context, 1))
                return;

            if (!config.EnableSuggestions)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Suggestions Disabled", message: "Suggesting of QOTDs for this profile using `/qotd` or `/suggest` has been disabled by staff."));
                return;
            }

            DiscordModalBuilder modal = Suggestions.EventHandlers.CreateSuggestion.GetQotdModal(config, context.Guild!.Name);

            await (context as SlashCommandContext)!.RespondWithModalAsync(modal);
        }


        [Command("qotd")]
        [Description("Suggest a Question Of The Day to be added. Unlike /suggest, this uses the selected/default profile.")]
        public static async Task QotdAsync(CommandContext context)
        { 
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            await SuggestAsync(context, config.ProfileId);
        }
    }
}
