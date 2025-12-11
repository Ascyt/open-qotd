using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Commands;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.EventHandlers.Suggestions;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;

namespace OpenQotd.EventHandlers
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

            bool editMessage = false;
            if (idArgs[0].StartsWith("edit_"))
            {
                editMessage = true;
                idArgs[0] = idArgs[0][5..];
            }

            if (idArgs[0].StartsWith("prompt-option-"))
                return;

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

                    await CreateSuggestionEventHandlers.SuggestQotdButtonClicked(args, int.Parse(idArgs[1]));
                    return;

                case "show-qotd-notes":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowQotdNotesButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;

                case "show-general-info":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowGeneralInfoButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;

                case "show-general-info-no-prompt":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowGeneralInfoNoPromptButtonClicked(args, int.Parse(idArgs[1]), editMessage);
                    return;

                case "show-qotd-info":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await QotdInfoButtonsEventHandlers.ShowQotdInfoButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]), editMessage);
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
                case "questions-add":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await QuestionsEventHandlers.QuestionsAddModalSubmitted(args, int.Parse(idArgs[1]));
                    return;
            }

            await RespondWithError(args, $"Unknown event: `{args.Interaction.Data.CustomId}`");
        }

        private static async Task<bool> HasExactlyNArguments(InteractionCreatedEventArgs args, string[] idArgs, int n)
        {
            if (idArgs.Length - 1 == n)
                return true;

            await RespondWithError(args, $"Component ID for `{idArgs[0]}` must have exactly {n} arguments (provided is {idArgs.Length - 1}).");
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
