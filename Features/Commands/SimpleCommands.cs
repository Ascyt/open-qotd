using DSharpPlus.Commands;
using System.ComponentModel;

namespace CustomQotd.Features.Commands
{
    public class SimpleCommands
    {
        [Command("sentquestions")]
        [Description("View all sent QOTDs")]
        public static async Task ViewSentQuestionsAsync(CommandContext context, 
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsBasic(context))
                return;

            await QuestionsCommand.ListQuestionsNoPermcheckAsync(context, Database.Entities.QuestionType.Sent, page);
        }
    }
}
