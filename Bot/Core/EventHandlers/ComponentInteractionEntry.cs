using DSharpPlus;
using DSharpPlus.EventArgs;
using OpenQotd.Core.UncategorizedCommands;

namespace OpenQotd.Core.EventHandlers
{
    public class ComponentInteractionEntry
    {
        public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            string[] idArgs = args.Id.Split("/");

            if (idArgs.Length == 0)
            {
                await Helpers.General.RespondWithError(args, "Interaction ID is empty");
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
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await Suggestions.EventHandlers.SuggestionNotification.SuggestionsAcceptButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggestions-deny":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await Suggestions.EventHandlers.SuggestionNotification.SuggestionsDenyButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await Suggestions.EventHandlers.CreateSuggestion.SuggestQotdButtonClicked(args, int.Parse(idArgs[1]));
                    return;

                case "show-qotd-notes":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowQotdNotesButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;

                case "show-general-info":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowGeneralInfoButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;

                case "show-general-info-no-prompt":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 1))
                        return;
                    await QotdInfoButtonsEventHandlers.ShowGeneralInfoNoPromptButtonClicked(args, int.Parse(idArgs[1]), editMessage);
                    return;

                case "show-qotd-info":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await QotdInfoButtonsEventHandlers.ShowQotdInfoButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]), editMessage);
                    return;

                case "help-select-profile":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 0))
                        return;

                    await HelpCommand.OnProfileSelectChanged(args);
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

            await Helpers.General.RespondWithError(args, $"Unknown event: `{args.Id}`");
        }

        public static async Task ModalSubmittedEvent(DiscordClient client, ModalSubmittedEventArgs args)
        {
            string[] idArgs = args.Interaction.Data.CustomId.Split("/");

            if (idArgs.Length == 0)
            {
                await Helpers.General.RespondWithError(args, "Interaction ID is empty");
                return;
            }

            switch (idArgs[0])
            {
                case "suggestions-deny":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await Suggestions.EventHandlers.SuggestionNotification.SuggestionsDenyReasonModalSubmitted(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await Suggestions.EventHandlers.CreateSuggestion.SuggestQotdModalSubmitted(args, int.Parse(idArgs[1]));
                    return;
                case "questions-add":
                    if (!await Helpers.General.HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await Questions.EventHandlers.General.QuestionsAddModalSubmitted(args, int.Parse(idArgs[1]));
                    return;
            }

            await Helpers.General.RespondWithError(args, $"Unknown event: `{args.Interaction.Data.CustomId}`");
        }
    }
}
