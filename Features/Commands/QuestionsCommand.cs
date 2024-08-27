using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CustomQotd.Features.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("view")]
        [Description("View a question using its ID.")]
        public static async Task ViewQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int QuestionId)
        {

        }

        [Command("add")]
        [Description("Add a question.")]
        public static async Task AddQuestionAsync(CommandContext context,
            [Description("The question to add.")] string question,
            [Description("The type of the question to add.")] QuestionType type)
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

        [Command("changetype")]
        [Description("Change the type of a question (eg. Sent->Accepted).")]
        public static async Task ChangeTypeOfQuestionAsync(CommandContext context,
            [Description("The ID of the question.")] int QuestionId,
            [Description("The type to set the question to.")] QuestionType type)
        {

        }

        [Command("remove")]
        [Description("Irreversably delete a question.")]
        public static async Task RemoveQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int QuestionId)
        {

        }

        [Command("list")]
        [Description("List all questions of a certain type.")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType type,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            const int itemsPerPage = 10;

            if (page < 1)
            {
                page = 1;
            }

            Question[] questions = null;
            int totalQuestions = -1;
            int totalPages = -1;
            async Task FetchDb()
            {
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
            }
            await FetchDb();

            await context.RespondAsync(
                MessageHelpers.GetListMessage(questions, $"{type} Questions List", page, totalPages)
                );

            if (totalPages == 0)
                return;

            DiscordMessage message = await context.GetResponseAsync();

            var result = await message.WaitForButtonAsync();

            while (!result.TimedOut)
            {
                bool messageDelete = false;
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
                    case "redirect":
                        page = totalPages;
                        messageDelete = true;
                        break;
                }

                await FetchDb();

                if (messageDelete)
                {
                    await message.DeleteAsync();
                    var newMessageContent = MessageHelpers.GetListMessage(questions, $"{type} Questions List", page, totalPages);
                    message = await context.Channel.SendMessageAsync(newMessageContent);
                }
                else
                {
                    DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();
                    MessageHelpers.EditListMessage(questions, $"{type} Questions List", page, totalPages, builder);
                    
                    await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
                }

                result = await message.WaitForButtonAsync();
            }
        }

        [Command("search")]
        [Description("Search all questions by a keyword.")]
        public static async Task SearchQuestionsAsync(CommandContext context,
            [Description("The search query (case-insensitive).")] string query,
            [Description("The type of questions to show (default all).")] QuestionType? type,
            [Description("The page of the listing (default 1).")] int page = 1)
        {

        }
    }
}
