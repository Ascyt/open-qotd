using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace OpenQotd.Features.Commands
{
    public class MyQuestionsCommand
    {
        [Command("myquestions")]
        [Description("View your submitted questions.")]
        [DirectMessageUsage(usage: DirectMessageUsage.RequireDMs)]
        public async Task MyQuestionsAsync(CommandContext context)
        {
            await context.RespondAsync("todo");
        }
    }
}