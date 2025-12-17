using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {
        [Command("addbulk")]
        [Description("Add multiple questions in bulk (one per line).")]
        public static async Task AddQuestionsBulkAsync(CommandContext context,
            [Description("A file containing the questions, each seperated by line-breaks.")] DiscordAttachment questionsFile,
            [Description("The type of the questions to add.")] QuestionType type)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.UserIsAdmin(context, config))
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

                    await Core.EventHandlers.Error.SendCommandErroredMessage(ex, context, "An error occurred while trying to fetch the file contents.", additionalEmbeds);
                    return;
                }
            }

            if (contents.Contains('\r'))
                contents = contents.Replace("\r", "");

            string[] lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (!await Api.IsWithinMaxQuestionsAmount(context, lines.Length))
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

            if (type == QuestionType.Suggested)
            {
                // Send suggestion notification messages
                foreach (Question question in questions)
                {
                    await Suggestions.Helpers.General.TryResetSuggestionMessageIfEnabledAsync(question, config, context.Guild!);
                    await Task.Delay(100); // Prevent rate-limit
                }
            }

            string body = $"Added {lines.Length} question{(lines.Length == 1 ? "" : "s")} ({Question.TypeToStyledString(type)}).";
            await context.RespondAsync(
                GenericEmbeds.Success("Added Bulk Questions", body)
                );
            await Logging.Api.LogUserAction(context, config, "Added Bulk Questions", message: body);
        }
    }
}
