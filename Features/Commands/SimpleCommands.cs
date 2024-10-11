using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
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

        [Command("feedback")]
        [Description("Leave feedback, suggestions or bugs for the developers of CustomQOTD.")]
            public static async Task FeedbackAsync(CommandContext context,
                [Description("The feedback, suggestion or bug.")] string feedback)
        {
            string contents = $"[FEEDBACK] @{context!.User.Username} ({context!.User.Id}):\n\t{feedback}\n\n";

            await File.AppendAllTextAsync("feedback.txt", contents);

            await Console.Out.WriteAsync(contents);

            DiscordEmbed responseEmbed = MessageHelpers.GenericSuccessEmbed("CustomQOTD feedback sent!",
                    $"> \"**{feedback}**\"");

            if (context is SlashCommandContext)
            {
                SlashCommandContext slashCommandcontext = context as SlashCommandContext;

                await slashCommandcontext.RespondAsync(responseEmbed, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(responseEmbed);
            }
        }
    }
}
