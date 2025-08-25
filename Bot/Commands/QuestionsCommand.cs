using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.EventHandlers;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Text;

namespace OpenQotd.Bot.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("view")]
        [Description("View a question using its ID.")]
        public static async Task ViewQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
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
                MessageHelpers.GenericEmbed(question.Text!, sb.ToString()));
        }

        [Command("add")]
        [Description("Add a question.")]
        public static async Task AddQuestionAsync(CommandContext context,
            [Description("The question to add.")] string question,
            [Description("The type of the question to add.")] QuestionType type)
        {
            Config? config = await CommandRequirements.TryGetConfig(context);

            if (config is null || !await CommandRequirements.UserIsAdmin(context, null) || !await CommandRequirements.WithinMaxQuestionsAmount(context, 1))
                return;

            if (!await Question.CheckTextValidity(question, context, config))
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

        [Command("addbulk")]
        [Description("Add multiple questions in bulk (one per line).")]
        public static async Task AddQuestionsBulkAsync(CommandContext context,
            [Description("A file containing the questions, each seperated by line-breaks.")] DiscordAttachment questionsFile,
            [Description("The type of the questions to add.")] QuestionType type)
        {
            Config? config = await CommandRequirements.TryGetConfig(context);

            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            await context.DeferResponseAsync();

            if (questionsFile.MediaType is null || !questionsFile.MediaType.Contains("text/plain"))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title: "Incorrect filetype", message: $"The questions file must be of type `text/plain` (not \"{(questionsFile.MediaType ?? "*null*")}\").\n\n" +
                    $"If this is a file containing questions seperated by line-breaks, make sure it is using UTF-8 encoding and has a `.txt` file extension."));
                return;
            }

            if (questionsFile.FileSize > 1024 * 1024)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title: "File too large", message: $"The questions file size cannot exceed 1MiB (yours is approx. {(questionsFile.FileSize / 1024f / 1024f):f2}MiB).")
                    );
                return;
            }

            string contents;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(questionsFile.Url);

                    response.EnsureSuccessStatusCode();

                    contents = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    await EventHandlers.EventHandlers.SendCommandErroredMessage(ex, context, "An error occurred while trying to fetch the file contents.");
                    return;
                }
            }

            if (contents.Contains('\r'))
                contents = contents.Replace("\r", "");

            string[] lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (!await CommandRequirements.WithinMaxQuestionsAmount(context, lines.Length))
                return;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!await Question.CheckTextValidity(lines[i], context, config, i+1))
                    return;
            }

            int startId = await Question.GetNextGuildDependentId(context.Guild!.Id);
            DateTime now = DateTime.UtcNow;
            IEnumerable<Question> questions = lines.Select((line, index) => new Question()
            {
                GuildId = context.Guild!.Id,
                GuildDependentId = startId + index,
                Type = type,
                Text = line,
                SubmittedByUserId = context.User.Id,
                Timestamp = now
            });

            using (var dbContext = new AppDbContext())
            {
                await dbContext.Questions.AddRangeAsync(questions);
                await dbContext.SaveChangesAsync();
            }

            string body = $"Added {lines.Length} question{(lines.Length == 1 ? "" : "s")} ({type}).";
            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Added Bulk Questions", body)
                );
        }

        [Command("changetype")]
        [Description("Change the type of a question (eg. Sent->Accepted).")]
        public static async Task ChangeTypeOfQuestionAsync(CommandContext context,
            [Description("The ID of the question.")] int questionId,
            [Description("The type to set the question to.")] QuestionType type)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
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

		[Command("changetypebulk")]
		[Description("Change the type of all questions of a certain type to another (eg. all Sent questions->Accepted).")]
		public static async Task ChangeTypeOfQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to change the type of.")] QuestionType fromType,
			[Description("The type to set all of those questions to.")] QuestionType toType)
		{
			if (!await CommandRequirements.UserIsAdmin(context, null))
				return;

			if (fromType == toType)
			{
				await context.RespondAsync(
					MessageHelpers.GenericErrorEmbed(message: $"Arguments `from_type` and `to_type` cannot be the same."));
				return;
			}

			ulong guildId = context.Guild!.Id;

			List<Question>? questions;
			using (var dbContext = new AppDbContext())
			{
				questions = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == fromType).ToListAsync();

				foreach (Question question in questions)
				{
					question.Type = toType;
				}

				await dbContext.SaveChangesAsync();
			}
			string body = $"Changed {questions.Count} question{(questions.Count == 1 ? "" : "s")} from **{fromType}** to **{toType}**.";

			await context.RespondAsync(
				MessageHelpers.GenericSuccessEmbed("Set Bulk Question Types", body)
				);
			await Logging.LogUserAction(context, "Set Bulk Question Types", body);
		}

		[Command("removebulk")]
		[Description("Remove all questions of a certain to stash or irreversably delete them if disabled.")]
		public static async Task RemoveQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to remove.")] QuestionType type)
		{
			if (!await CommandRequirements.UserIsAdmin(context, null))
				return;

			ulong guildId = context.Guild!.Id;

			List<Question>? questions;
            Config? config;
			using (var dbContext = new AppDbContext())
			{
				questions = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == type).ToListAsync();

                config = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();
                if (config == null)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Config Not Found", message: "The bot configuration could not be found."));
                    return;
                }

				if (config.EnableDeletedToStash && type != QuestionType.Stashed) 
				{
                    foreach (Question question in questions)
					{
						question.Type = QuestionType.Stashed;
					}
				}
                else
                {
					dbContext.Questions.RemoveRange(questions);
				}

                await dbContext.SaveChangesAsync();
			}
			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type **{type}**.";

			string title = config.EnableDeletedToStash && type != QuestionType.Stashed ? "Removed Bulk Questions to Stash" : "Removed Bulk Questions";

			await context.RespondAsync(
				MessageHelpers.GenericSuccessEmbed(title, body)
				);
			await Logging.LogUserAction(context, title, body);
		}
        [Command("clearstash")]
        [Description("Remove all questions of Stashed type.")]
        public static async Task ClearStashAsync(CommandContext context)
        {
			if (!await CommandRequirements.UserIsAdmin(context, null))
				return;

			ulong guildId = context.Guild!.Id;

			List<Question>? questions;
			Config? config;
			using (var dbContext = new AppDbContext())
			{
				questions = await dbContext.Questions.Where(q => q.GuildId == guildId && q.Type == QuestionType.Stashed).ToListAsync();

				config = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();
				if (config == null)
				{
					await context.RespondAsync(
						MessageHelpers.GenericErrorEmbed(title: "Config Not Found", message: "The bot configuration could not be found."));
					return;
				}

				dbContext.Questions.RemoveRange(questions);
				await dbContext.SaveChangesAsync();
			}
			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type **{QuestionType.Stashed}**.";

			string title = "Cleared Stash";

			await context.RespondAsync(
				MessageHelpers.GenericSuccessEmbed(title, body)
				);
			await Logging.LogUserAction(context, title, body);
		}

		[Command("remove")]
        [Description("Remove a question to stash or irreversably delete it if disabled.")]
        public static async Task RemoveQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            Config? config;
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

                config = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();
                if (config == null)
                {
					await context.RespondAsync(
						MessageHelpers.GenericErrorEmbed(title: "Config Not Found", message: "The bot configuration could not be found."));
					return;
				}

                if (config.EnableDeletedToStash && question.Type != QuestionType.Stashed)
                {
                    question.Type = QuestionType.Stashed;
				}
                else
                {
                    dbContext.Questions.Remove(question);
                }
                await dbContext.SaveChangesAsync();
            }

            string title = config.EnableDeletedToStash && question.Type != QuestionType.Stashed ? "Removed Question to Stash" : "Removed Question";

			await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed(title, body)
                );
            await Logging.LogUserAction(context, title, body);
        }

        [Command("list")]
        [Description("List all questions of a certain type.")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            await ListQuestionsNoPermcheckAsync(context, type, page);
        }
        public static async Task ListQuestionsNoPermcheckAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            const int itemsPerPage = 10;

            await MessageHelpers.ListMessageComplete<Question>(context, page, type is null ? $"Questions List" : $"{type} Questions List", async Task<(Question[], int, int, int)> (int page) =>
            {
                using (var dbContext = new AppDbContext())
                {
                    IQueryable<Question> sqlQuery;
                    if (type is null)
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.GuildId == context.Guild!.Id)
                            .OrderBy(q => q.Type);
                    }
                    else
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.GuildId == context.Guild!.Id && q.Type == type);
                    }

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
                        totalElements, totalPages, itemsPerPage);
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
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            const int itemsPerPage = 10;
            await MessageHelpers.ListMessageComplete<Question>(context, page, $"{(type != null ? $"{type} " : "")}Questions Search for \"{query}\"", async Task<(Question[], int, int, int)> (int page) =>
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

                    return (questions, totalQuestions, totalPages, itemsPerPage);
                }
            });
        }
    }
}
