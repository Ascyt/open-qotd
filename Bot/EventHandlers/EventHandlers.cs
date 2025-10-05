using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Commands;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.EventHandlers.Suggestions;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;

namespace OpenQotd.Bot.EventHandlers
{
    public class EventHandlers
    {
        public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            string[] idArgs = args.Id.Split("/");

            if (idArgs.Length == 0)
            {
                await RespondWithError(args, "Interaction ID is empty");
                return;
            }

            switch (idArgs[0])
            {
                case "suggestions-accept":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionNotificationsEventHandlers.SuggestionsAcceptButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggestions-deny":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionNotificationsEventHandlers.SuggestionsDenyButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await CreateSuggestionEventHandlers.SuggestQotdButtonClicked(client, args, int.Parse(idArgs[1]));
                    return;

                case "show-qotd-notes":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowQotdNotesButtonClicked(client, args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;

                case "forward":
                case "backward":
                case "first":
                case "last":
                case "redirect":
                case "reroll":
                case "confirm_choice":
                case "cancel_choice":
                    return;
            }

            await RespondWithError(args, $"Unknown event: `{args.Id}`");
        }

        public static async Task ModalSubmittedEvent(DiscordClient client, ModalSubmittedEventArgs args)
        {
            string[] idArgs = args.Interaction.Data.CustomId.Split("/");

            if (idArgs.Length == 0)
            {
                await RespondWithError(args, "Interaction ID is empty");
                return;
            }

            switch (idArgs[0])
            {
                case "suggestions-deny":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionNotificationsEventHandlers.SuggestionsDenyReasonModalSubmitted(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await CreateSuggestionEventHandlers.SuggestQotdModalSubmitted(args, int.Parse(idArgs[1]));
                    return;
            }

            await RespondWithError(args, $"Unknown event: `{args.Interaction.Data.CustomId}`");
        }

        private static async Task<bool> HasExactlyNArguments(InteractionCreatedEventArgs args, string[] idArgs, int n)
        {
            if (idArgs.Length - 1 == n)
                return true;

            await RespondWithError(args, $"Component ID for `{idArgs[0]}` must have exactly {n} arguments (provided is {idArgs.Length}).");
            return false;
        }

        public static async Task RespondWithError(InteractionCreatedEventArgs args, string message, string? title=null)
        {
            DiscordEmbed errorEmbed = GenericEmbeds.Error(title: title ?? "Error", message: message);

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(errorEmbed)
                .AsEphemeral());
        }
    }
}
