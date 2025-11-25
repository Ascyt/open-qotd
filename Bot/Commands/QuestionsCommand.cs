using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using System.ComponentModel;
using System.Text;

namespace OpenQotd.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("view")]
        [Description("View a question using its ID.")]
        public static async Task ViewQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId)
                    .FirstOrDefaultAsync();
            }

            if (question == null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                return;
            }

            await context.RespondAsync(GetQuestionsViewResponse(config, question));
        }

        public static DiscordMessageBuilder GetQuestionsViewResponse(Config config, Question question)
        {
            DiscordMessageBuilder response = new();

            StringBuilder generalInfo = new();
            generalInfo.AppendLine($"Belongs to profile: **{config.ProfileName}**");
            generalInfo.AppendLine($"ID: `{question.GuildDependentId}`");
            generalInfo.AppendLine($"Type: {Question.TypeToStyledString(question.Type)}");
            generalInfo.AppendLine();
            generalInfo.AppendLine($"Submitted by: <@{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)");
            generalInfo.AppendLine($"Submitted at: {DSharpPlus.Formatter.Timestamp(question.Timestamp, DSharpPlus.TimestampFormat.ShortDateTime)}");
            if (question.AcceptedByUserId is not null || question.AcceptedTimestamp is not null)
            {
                generalInfo.AppendLine();
                if (question.AcceptedByUserId is not null)
                    generalInfo.AppendLine($"Accepted by: <@{question.AcceptedByUserId}> (`{question.AcceptedByUserId}`)");
                if (question.AcceptedTimestamp is not null)
                    generalInfo.AppendLine($"Accepted at: {DSharpPlus.Formatter.Timestamp(question.AcceptedTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
            }
            if (question.SentTimestamp is not null || question.SentNumber is not null)
            {
                generalInfo.AppendLine();
                if (question.SentTimestamp is not null)
                    generalInfo.AppendLine($"Sent at: {DSharpPlus.Formatter.Timestamp(question.SentTimestamp.Value, DSharpPlus.TimestampFormat.ShortDateTime)}");
                if (question.SentNumber is not null)
                    generalInfo.AppendLine($"Sent number: **{question.SentNumber}**");
            }
            response.AddEmbed(GenericEmbeds.Info(title: "General", message: generalInfo.ToString()));

            response.AddEmbed(GenericEmbeds.Info(title: "Contents", message: question.Text!).WithFooter($"Written by the submittor. Gets sent as the main {config.QotdShorthandText} body."));

            if (!string.IsNullOrWhiteSpace(question.Notes))
                response.AddEmbed(GenericEmbeds.Info(title: "Additional Notes", message: question.Notes).WithFooter($"Written by the submittor. Gets shown when a button under the {config.QotdShorthandText} is pressed."));

            if (!string.IsNullOrWhiteSpace(question.SuggesterAdminOnlyInfo))
                response.AddEmbed(GenericEmbeds.Info(title: "Admin-Only Info", message: question.SuggesterAdminOnlyInfo).WithFooter("Written by the submittor. Visible to staff only."));

            if (!string.IsNullOrWhiteSpace(question.ThumbnailImageUrl))
                response.AddEmbed(GenericEmbeds.Info(title: "Thumbnail Image", message: $"URL: <{question.ThumbnailImageUrl}>")
                    .WithImageUrl(question.ThumbnailImageUrl)
                    .WithFooter("Thumbnail image URL, as provided by the submittor. Gets shown as a small image above the main body."));

            return response;
        }

        [Command("add")]
        [Description("Add a question.")]
        public static async Task AddQuestionAsync(CommandContext context,
            [Description("The question to add.")] string question,
            [Description("The type of the question to add.")] QuestionType type)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            if (!await CommandRequirements.IsWithinMaxQuestionsAmount(context, 1))
                return;

            ulong guildId = context.Guild!.Id;
            ulong submittedByUserId = context.User.Id;

            Question newQuestion;
            newQuestion = new Question()
            {
                ConfigId = config.Id,
                GuildId = guildId,
                GuildDependentId = await Question.GetNextGuildDependentId(config),
                Type = type,
                Text = question,
                SubmittedByUserId = submittedByUserId,
                Timestamp = DateTime.UtcNow
            };
            if (!await Question.CheckQuestionValidity(newQuestion, context, config))
                return;

            using (AppDbContext dbContext = new())
            {
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();
            }

            string body = newQuestion.ToString(longVersion: true);

			await context.RespondAsync(
                GenericEmbeds.Success("Added Question", body)
                );
            await Logging.LogUserAction(context, config, "Added Question", message: body);
        }

        [Command("addbulk")]
        [Description("Add multiple questions in bulk (one per line).")]
        public static async Task AddQuestionsBulkAsync(CommandContext context,
            [Description("A file containing the questions, each seperated by line-breaks.")] DiscordAttachment questionsFile,
            [Description("The type of the questions to add.")] QuestionType type)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            await context.DeferResponseAsync();

            if (questionsFile.MediaType is null || !questionsFile.MediaType.Contains("text/plain"))
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Incorrect filetype", message: $"The questions file must be of type `text/plain` (not \"{(questionsFile.MediaType ?? "*null*")}\").\n\n" +
                    $"If this is a file containing questions seperated by line-breaks, make sure it is using UTF-8 encoding and has a `.txt` file extension."));
                return;
            }

            if (questionsFile.FileSize > 1024 * 1024)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "File too large", message: $"The questions file size cannot exceed 1MiB (yours is approx. {(questionsFile.FileSize / 1024f / 1024f):f2}MiB).")
                    );
                return;
            }

            string contents;
            using (HttpClient client = new())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(questionsFile.Url);

                    response.EnsureSuccessStatusCode();

                    contents = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    List<DiscordEmbed> additionalEmbeds = [];
                    if (ex is InvalidOperationException)
                    {
                        additionalEmbeds.Add(
                            GenericEmbeds.Warning(title:"Hint", message:
                            $"If you're encountering the error \"The character set provided in ContentType is invalid\", " +
                            $"it likely means that there are characters in your file that are invalid in your encoding or that your encoding is not supported.\n" +
                            $"\n" +
                            $"This is a common issue that can occurr when you've exported a Google Docs file (or similar) to `.txt`. " +
                            $"A simple fix for this is to manually create a `.txt` file using **Windows Notepad**, **Notepad++** or a similar plain text editor, paste your text into there and then use that file. " +
                            $"Also, ensure that it says \"UTF-8\" (not \"UTF-8-BOM\" or similar) and something along the lines of \"plain text file\" at the bottom of your open `.txt` file.\n" +
                            $"\n" +
                            $"If you are still experiencing issues with this, don't hesitate to let me know! I'll do my best to be quick to help with any issues.")
                            );
                    }

                    await EventHandlers.ErrorEventHandlers.SendCommandErroredMessage(ex, context, "An error occurred while trying to fetch the file contents.", additionalEmbeds);
                    return;
                }
            }

            if (contents.Contains('\r'))
                contents = contents.Replace("\r", "");

            string[] lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (!await CommandRequirements.IsWithinMaxQuestionsAmount(context, lines.Length))
                return;

            int startId = await Question.GetNextGuildDependentId(config);
            DateTime now = DateTime.UtcNow;
            IEnumerable<Question> questions = lines.Select((line, index) => new Question()
            {
                ConfigId = config.Id,
                GuildId = context.Guild!.Id,
                GuildDependentId = startId + index,
                Type = type,
                Text = line,
                SubmittedByUserId = context.User.Id,
                Timestamp = now
            });
            int lineNumber = 1;
            foreach (Question question in questions)
            {
                if (!await Question.CheckQuestionValidity(question, context, config, lineNumber))
                    return;
                lineNumber++;
            }

            using (AppDbContext dbContext = new())
            {
                await dbContext.Questions.AddRangeAsync(questions);
                await dbContext.SaveChangesAsync();
            }

            string body = $"Added {lines.Length} question{(lines.Length == 1 ? "" : "s")} ({Question.TypeToStyledString(type)}).";
            await context.RespondAsync(
                GenericEmbeds.Success("Added Bulk Questions", body)
                );
            await Logging.LogUserAction(context, config, "Added Bulk Questions", message: body);
        }

        [Command("changetype")]
        [Description("Change the type of a question (eg. Sent->Accepted).")]
        public static async Task ChangeTypeOfQuestionAsync(CommandContext context,
            [Description("The ID of the question.")] int questionId,
            [Description("The type to set the question to.")] QuestionType type)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId).FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Question Not Found", message: $"The question with ID `{questionId}` could not be found."));
                    return;
                }

                body = $"\n> {Question.TypeToStyledString(question.Type)} → {Question.TypeToStyledString(type)}";

                question.Type = type;

                body = question.ToString() + body;

                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                GenericEmbeds.Success("Set Question Type", body)
                );
            await Logging.LogUserAction(context, config, "Set Question Type", message: body);
		}

		[Command("changetypebulk")]
		[Description("Change the type of all questions of a certain type to another (eg. all Sent questions->Accepted).")]
		public static async Task ChangeTypeOfQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to change the type of.")] QuestionType fromType,
			[Description("The type to set all of those questions to.")] QuestionType toType)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            if (fromType == toType)
			{
				await context.RespondAsync(
					GenericEmbeds.Error(message: $"Arguments `from_type` and `to_type` cannot be the same."));
				return;
			}

			ulong guildId = context.Guild!.Id;

			List<Question>? questions;
			using (AppDbContext dbContext = new())
			{
				questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == fromType).ToListAsync();

				foreach (Question question in questions)
				{
					question.Type = toType;
				}

				await dbContext.SaveChangesAsync();
			}
			string body = $"Changed {questions.Count} question{(questions.Count == 1 ? "" : "s")} from {Question.TypeToStyledString(fromType)} to {Question.TypeToStyledString(toType)}.";

			await context.RespondAsync(
				GenericEmbeds.Success("Set Bulk Question Types", body)
				);
			await Logging.LogUserAction(context, config, "Set Bulk Question Types", message: body);
		}

		[Command("removebulk")]
		[Description("Remove all questions of a certain to stash or irreversably delete them if disabled.")]
		public static async Task RemoveQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to remove.")] QuestionType type)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

			List<Question>? questions;
			using (AppDbContext dbContext = new())
			{
				questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == type).ToListAsync();

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
			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type {Question.TypeToStyledString(type)}.";

			string title = config.EnableDeletedToStash && type != QuestionType.Stashed ? "Removed Bulk Questions to Stash" : "Removed Bulk Questions";

			await context.RespondAsync(
				GenericEmbeds.Success(title, body)
				);
			await Logging.LogUserAction(context, config, title, message: body);
		}
        [Command("clearstash")]
        [Description("Remove all questions of Stashed type.")]
        public static async Task ClearStashAsync(CommandContext context)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            ulong guildId = context.Guild!.Id;

			List<Question>? questions;
			using (AppDbContext dbContext = new())
			{
				questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Stashed).ToListAsync();

				dbContext.Questions.RemoveRange(questions);
				await dbContext.SaveChangesAsync();
			}
			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type {Question.TypeToStyledString(QuestionType.Stashed)}.";

			string title = "Cleared Stash";

			await context.RespondAsync(
				GenericEmbeds.Success(title, body)
				);
			await Logging.LogUserAction(context, config, title, body);
		}

		[Command("remove")]
        [Description("Remove a question to stash or irreversably delete it if disabled.")]
        public static async Task RemoveQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId).FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                    return;
                }
                body = question.ToString();

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
                GenericEmbeds.Success(title, body)
                );
            await Logging.LogUserAction(context, config, title, body);
        }

        [Command("list")]
        [Description("List all questions.")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            await ListQuestionsNoPermcheckAsync(context, config, type, page);
        }
        public static async Task ListQuestionsNoPermcheckAsync(CommandContext context, Config config, QuestionType? type = null, int page = 1)
        {
            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;

            await ListMessages.SendNew(context, page, type is null ? $"{config.QotdShorthandText} Questions List" : $"{type} {config.QotdShorthandText} Questions List", 
                async Task<PageInfo<Question>> (int page) =>
                {
                    using AppDbContext dbContext = new();

                    IQueryable<Question> sqlQuery;
                    if (type is null)
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.ConfigId == config.Id)
                            .OrderBy(q => q.Type)
                            .ThenByDescending(q => q.Timestamp)
                            .ThenByDescending(q => q.Id);
                    }
                    else
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.ConfigId == config.Id && q.Type == type)
                            .OrderByDescending(q => q.Timestamp)
                            .ThenByDescending(q => q.Id);
                    }

                    // Get the total number of questions
                    int totalElements = await sqlQuery
                        .CountAsync();

                    // Calculate the total number of pages
                    int totalPages = (int)Math.Ceiling(totalElements / (double)itemsPerPage);

                    Question[] currentPageQuestions = await sqlQuery
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync();

                    PageInfo<Question> pageInfo = new()
                    {
                        Elements = currentPageQuestions,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalElements,
                        TotalPagesCount = totalPages,
                    };

                    // Fetch the questions for the current page
                    return pageInfo;
                });
        }

        [Command("search")]
        [Description("Search all questions by a keyword.")]
        public static async Task SearchQuestionsAsync(CommandContext context,
            [Description("The search query (case-insensitive).")] string query,
            [Description("The type of questions to show (default all).")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNew<Question>(context, page, $"{(type != null ? $"{type} " : "")}Questions Search for \"{query}\"", 
                async Task<PageInfo<Question>> (int page) =>
                {
                    using AppDbContext dbContext = new();

                    // Build the base query
                    IQueryable<Question> sqlQuery = dbContext.Questions
                        .Where(q => q.ConfigId == config.Id && (type == null || q.Type == type))
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

                    PageInfo<Question> pageInfo = new()
                    {
                        Elements = questions,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalQuestions,
                        TotalPagesCount = totalPages,
                    };

                    return pageInfo;
                });
        }
    }
}
