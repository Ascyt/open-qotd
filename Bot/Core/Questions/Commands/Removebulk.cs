using System.ComponentModel;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {
		[Command("removebulk")]
		[Description("Remove all questions of a certain type to stash or irreversibly delete them if disabled.")]
		public static async Task RemoveQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to remove.")] QuestionType type)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.UserIsAdmin(context, config))
                return;

			List<Question>? questions;
            Dictionary<int, QuestionType> originalTypes;
			using (AppDbContext dbContext = new())
			{
				questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == type).ToListAsync();

                originalTypes = new(questions.Count);
                foreach (Question question in questions)
                {
                    originalTypes.Add(question.GuildDependentId, question.Type);
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

            foreach (Question question in questions)
            {
                if (originalTypes[question.GuildDependentId] == QuestionType.Suggested)
                {
                    // Set suggestion message to modified state
                    await Helpers.Suggestions.General.TrySetSuggestionMessageToModifiedIfEnabledAsync(question, config, context.Guild!);
                    await Task.Delay(100); // Prevent rate-limit
                }
            }

			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type {Question.TypeToStyledString(type)}.";

			string title = config.EnableDeletedToStash && type != QuestionType.Stashed ? "Removed Bulk Questions to Stash" : "Removed Bulk Questions";

			await context.RespondAsync(
				GenericEmbeds.Success(title, body)
				);
			await Logging.Api.LogUserAction(context, config, title, message: body);
		}
    }
}
