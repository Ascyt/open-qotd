using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("add")]
        public static async Task AddQuestionAsync(CommandContext context,
            [System.ComponentModel.Description("The question to add.")] string question,
            [System.ComponentModel.Description("The type of the question to add.")] QuestionType type)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            ulong guildId = context.Guild!.Id;
            ulong submittedByUserId = context.User.Id;

            Question newQuestion;

            using (var dbContext = new AppDbContext())
            {
                newQuestion = new Question()
                {
                    GuildId = guildId,
                    GuildDependentId = await Question.GetNextGuildDependentId(guildId),
                    Type = type,
                    Text = question,
                    SubmittedByUserId = submittedByUserId,
                    Timestamp = DateTime.UtcNow
                };
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();
            }

            string body = $"\"**{question}**\" ({type}, ID: `{newQuestion.GuildDependentId}`)";
            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Added question", body)
                );
            await Logging.LogUserAction(context, "Added question", body);
        }

        [Command("list")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [System.ComponentModel.Description("The type of questions to show")] QuestionType type,
            [System.ComponentModel.Description("The page of the listing (default 1)")] int page = 1)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            const int itemsPerPage = 10;

            if (page < 1)
            {
                page = 1;
            }

            Question[] questions;
            int totalQuestions;
            int totalPages;
            using (var dbContext = new AppDbContext())
            {
                // Get the total number of questions
                totalQuestions = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.Type == type)
                    .CountAsync();

                // Calculate the total number of pages
                totalPages = (int)Math.Ceiling(totalQuestions / (double)itemsPerPage);

                // Fetch the questions for the current page
                questions = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.Type == type)
                    .Skip((page - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToArrayAsync();
            }

            await context.RespondAsync(
                MessageHelpers.GetListMessage(questions, $"{type} Questions List", page, totalPages)
                );

            DiscordMessage message = await context.GetResponseAsync();
            var result = await message.WaitForButtonAsync();

            while (!result.TimedOut)
            {
                await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);

                switch (result.Result.Id)
                {
                    case "first":
                        page = 1;
                        break;
                    case "backward":
                        page--;
                        break;
                    case "forward":
                        page++;
                        break;
                    case "last":
                        page = totalPages;
                        break;
                }

                await context.EditFollowupAsync(
                    message.Id,
                    MessageHelpers.GetListMessage(questions, $"{type} Questions List", page, totalPages)
                ); 
                
                result = await message.WaitForButtonAsync();
            }
        }
    }
}
