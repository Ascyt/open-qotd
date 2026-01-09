using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Core.UncategorizedCommands;

namespace OpenQotd.Core.EventHandlers
{
    public class ComponentInteractionEntry
    {
        public static async Task ComponentInteractionCreatedAsync(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            string[] idArgs = args.Id.Split("/");

            if (idArgs.Length == 0)
            {
                await Helpers.General.RespondWithErrorAsync(args, "Interaction ID is empty");
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

            try
            {
                switch (idArgs[0])
                {
                    case "suggestions-accept":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 2))
                            return;

                        await Suggestions.EventHandlers.SuggestionNotification.SuggestionsAcceptButtonClickedAsync(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                        return;
                    case "suggestions-deny":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 2))
                            return;

                        await Suggestions.EventHandlers.SuggestionNotification.SuggestionsDenyButtonClicked(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                        return;
                    case "suggest-qotd":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 1))
                            return;

                        await Suggestions.EventHandlers.CreateSuggestion.SuggestQotdButtonClickedAsync(args, int.Parse(idArgs[1]));
                        return;

                    case "show-qotd-notes":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 2))
                            return;
                        await QotdInfoButtonsEventHandlers.ShowQotdNotesButtonClickedAsync(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                        return;

                    case "show-general-info":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 2))
                            return;
                        await QotdInfoButtonsEventHandlers.ShowGeneralInfoButtonClickedAsync(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                        return;

                    case "show-general-info-no-prompt":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 1))
                            return;
                        await QotdInfoButtonsEventHandlers.ShowGeneralInfoNoPromptButtonClickedAsync(args, int.Parse(idArgs[1]), editMessage);
                        return;

                    case "show-qotd-info":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 2))
                            return;

                        await QotdInfoButtonsEventHandlers.ShowQotdInfoButtonClickedAsync(args, int.Parse(idArgs[1]), int.Parse(idArgs[2]), editMessage);
                        return;

                    case "help-select-profile":
                        if (!await Helpers.General.HasExactlyNArgumentsAsync(args, idArgs, 0))
                            return;

                        await HelpCommand.OnProfileSelectChangedAsync(args);
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
            }
            catch (RateLimitException ex)
            {
                await Core.Helpers.General.LogRateLimitExceptionAsync(ex, contextInfo: $"EventHandlers.ComponentInteractionCreated for interaction ID `{args.Id}`");
            }


            await Helpers.General.RespondWithErrorAsync(args, $"Unknown event: `{args.Id}`");
        }
    }
}
