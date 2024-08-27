using CustomQotd.Database.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace CustomQotd.Features.Commands
{
    [Command("suggestions")]
    public class SuggestionsCommands
    {
        [Command("accept")]
        [Description("Accept a suggestion.")]
        public static async Task AcceptSuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {

        }

        public static async Task AcceptSuggestionNoContextAsync(Question question, DiscordMessage suggestionMessage)
        {
            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.WithContent("accepted");

            await suggestionMessage.ModifyAsync(messageBuilder);
        }

        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId,
        [Description("The reason why the suggestion is denied, which will be sent to the user.")] string reason)
        {

        }
        public static async Task DenySuggestionNoContextAsync(Question question, DiscordMessage suggestionMessage, string reason)
        {
            await suggestionMessage.ModifyAsync("denied: " + reason);
        }

        // TODO: acceptall, denyall
    }
}
