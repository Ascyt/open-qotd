using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace OpenQotd.Core.EventHandlers
{
    public class ModalSubmittedEntry
    {
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
