using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Text;

namespace CustomQotd.Features.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("view")]
        [Description("View a question using its ID.")]
        public static async Task ViewQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            Question? question;
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.GuildDependentId == questionId)
                    .FirstOrDefaultAsync();
            }

            if (question == null)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                return;
            }

            StringBuilder sb = new();

            sb.AppendLine($"ID: `{question.GuildDependentId}`");
            sb.AppendLine($"Type: **{question.Type}**");
            sb.AppendLine();
            sb.AppendLine($"Submitted by: <@{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)");
            sb.AppendLine($"Submitted at: {DSharpPlus.Formatter.Timestamp(question.Timestamp, DSharpPlus.TimestampFormat.ShortDateTime)}");
            if (question.AcceptedByUserId != null || question.AcceptedTimestamp != null)
            {
                sb.AppendLine();
                if (question.AcceptedByUserId != null)
                    sb.AppendLine($"Accepted by: <@{question.AcceptedByUserId}> (`{question.AcceptedByUserId}`)");
                if (question.AcceptedTimestamp != null)
                    sb.AppendLine($"Accepted at: {DSharpPlus.Formatter.Timestamp(question.AcceptedTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
            }
            if (question.SentTimestamp != null || question.SentNumber != null)
            {
                sb.AppendLine();
                if (question.SentTimestamp != null)
                    sb.AppendLine($"Sent at: {DSharpPlus.Formatter.Timestamp(question.SentTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
                if (question.SentNumber != null)
                    sb.AppendLine($"Sent number: **{question.SentNumber}**");
            }

            await context.RespondAsync(
                MessageHelpers.GenericEmbed(question.Text, sb.ToString()));
        }

        [Command("add")]
        [Description("Add a question.")]
        public static async Task AddQuestionAsync(CommandContext context,
            [Description("The question to add.")] string question,
            [Description("The type of the question to add.")] QuestionType type)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            if (!await Question.CheckTextValidity(question, context))
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
                MessageHelpers.GenericSuccessEmbed("Added Question", body)
                );
            await Logging.LogUserAction(context, "Added Question", body);
        }

        [Command("changetype")]
        [Description("Change the type of a question (eg. Sent->Accepted).")]
        public static async Task ChangeTypeOfQuestionAsync(CommandContext context,
            [Description("The ID of the question.")] int questionId,
            [Description("The type to set the question to.")] QuestionType type)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions.Where(q => q.GuildId == guildId && q.GuildDependentId == questionId).FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Question Not Found", message: $"The question with ID `{questionId}` could not be found."));
                    return;
                }

                body = $"\n> **{question.Type}** → **{type}**";

                question.Type = type;

                body = question.ToString() + body;

                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Set Question Type", body)
                );
            await Logging.LogUserAction(context, "Set Question Type", body);
        }

        [Command("remove")]
        [Description("Irreversably delete a question.")]
        public static async Task RemoveQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions.Where(q => q.GuildId == guildId && q.GuildDependentId == questionId).FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                    return;
                }
                body = question.ToString();

                dbContext.Questions.Remove(question);
                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Removed Question", body)
                );
            await Logging.LogUserAction(context, "Removed Question", body);
        }

        [Command("list")]
        [Description("List all questions of a certain type.")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType type,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            await ListQuestionsNoPermcheckAsync(context, type, page);
        }
        public static async Task ListQuestionsNoPermcheckAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType type,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            const int itemsPerPage = 10;

            await MessageHelpers.ListMessageComplete<Question>(context, page, $"{type} Questions List", async Task<(Question[], int, int)> (int page) =>
            {
                using (var dbContext = new AppDbContext())
                {
                    var sqlQuery = dbContext.Questions
                        .Where(q => q.GuildId == context.Guild!.Id && q.Type == type);

                    // Get the total number of questions
                    int totalElements = await sqlQuery
                        .CountAsync();

                    // Calculate the total number of pages
                    int totalPages = (int)Math.Ceiling(totalElements / (double)itemsPerPage);

                    // Fetch the questions for the current page
                    return (await sqlQuery
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync(),
                        totalElements, totalPages);
                }
            });
        }

        [Command("search")]
        [Description("Search all questions by a keyword.")]
        public static async Task SearchQuestionsAsync(CommandContext context,
            [Description("The search query (case-insensitive).")] string query,
            [Description("The type of questions to show (default all).")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsAdmin(context))
                return;

            const int itemsPerPage = 10;
            await MessageHelpers.ListMessageComplete<Question>(context, page, $"{(type != null ? $"{type} " : "")}Questions Search for \"{query}\"", async Task<(Question[], int, int)> (int page) =>
            {

                using (var dbContext = new AppDbContext())
                {
                    // Build the base query
                    var sqlQuery = dbContext.Questions
                        .Where(q => q.GuildId == context.Guild!.Id && (type == null || q.Type == type))
                        .Where(q => EF.Functions.Like(q.Text, $"%{query}%"));

                    // Get the total number of questions
                    int totalQuestions = await sqlQuery.CountAsync();

                    // Calculate the total number of pages
                    int totalPages = (int)Math.Ceiling(totalQuestions / (double)itemsPerPage);

                    // Fetch the questions for the current page
                    Question[] questions = await sqlQuery
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync();

                    return (questions, totalQuestions, totalPages);
                }
            });
        }
    }
}
