using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using CustomQotd.Migrations;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
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

        public static async Task AcceptSuggestionNoContextAsync(Question question, DiscordMessage suggestionMessage, InteractivityResult<ComponentInteractionCreatedEventArgs> result, string embedBody)
        {
            using (var dbContext = new AppDbContext())
            {
                Question? modifyQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (modifyQuestion == null)
                {
                    await suggestionMessage.Channel!.SendMessageAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Question Not Found", message: $"The question with ID `{question.GuildDependentId}` could not be found."));
                    return;
                }

                modifyQuestion.Type = QuestionType.Accepted;
                modifyQuestion.AcceptedByUserId = result.Result.User.Id;
                modifyQuestion.AcceptedTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                question = modifyQuestion;
            }

            DiscordMessageBuilder messageBuilder = new();

            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Accepted", embedBody + 
                $"\n\nAccepted by: {result.Result.User.Mention}", color:"#20ff20"));

            await suggestionMessage.ModifyAsync(messageBuilder);

            await Logging.LogUserAction(suggestionMessage.Channel!.Guild.Id, suggestionMessage.Channel, result.Result.User, "Accepted Suggestion", question.ToString());
        }

        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId,
        [Description("The reason why the suggestion is denied, which will be sent to the user.")] string reason)
        {

        }
        public static async Task DenySuggestionNoContextAsync(Question question, DiscordMessage suggestionMessage, InteractivityResult<ComponentInteractionCreatedEventArgs> result, string embedBody, string reason)
        {
            using (var dbContext = new AppDbContext())
            {
                Question? removeQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (removeQuestion == null)
                {
                    await suggestionMessage.Channel!.SendMessageAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Question Not Found", message: $"The question with ID `{question.GuildDependentId}` could not be found."));
                    return;
                }

                dbContext.Questions.Remove(removeQuestion); 

                await dbContext.SaveChangesAsync();
            }

            DiscordMessageBuilder messageBuilder = new();

            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Denied", embedBody + 
                $"\n\nDenied by: {result.Result.User.Mention}\nReason: **\"{reason}\"**", color: "#ff2020"));

            await suggestionMessage.ModifyAsync(messageBuilder);

            await Logging.LogUserAction(suggestionMessage.Channel!.Guild.Id, suggestionMessage.Channel, result.Result.User, "Denied Suggestion", $"{question.ToString()}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
        }

        // TODO: acceptall, denyall
    }
}
