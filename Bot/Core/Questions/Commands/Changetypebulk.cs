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
		[Command("changetypebulk")]
		[Description("Change the type of all questions of a certain type to another (eg. all Sent questions->Accepted).")]
		public static async Task ChangeTypeOfQuestionsBulkAsync(CommandContext context,
			[Description("The type of the questions to change the type of.")] QuestionType fromType,
			[Description("The type to set all of those questions to.")] QuestionType toType)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.UserIsAdmin(context, config))
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
            
            if (fromType == QuestionType.Suggested && toType != QuestionType.Suggested)
            {
                foreach (Question question in questions)
                {
                    // Set suggestion message to modified state
                    await Helpers.Suggestions.General.TrySetSuggestionMessageToModifiedIfEnabledAsync(question, config, context.Guild!);
                    await Task.Delay(100); // Prevent rate-limit
                }
            }
            else if (fromType != QuestionType.Suggested && toType == QuestionType.Suggested)
            {
                foreach (Question question in questions)
                {
                    // Send suggestion notification message
                    await Helpers.Suggestions.General.TryResetSuggestionMessageIfEnabledAsync(question, config, context.Guild!);
                    await Task.Delay(100); // Prevent rate-limit
                }
            }

			string body = $"Changed {questions.Count} question{(questions.Count == 1 ? "" : "s")} from {Question.TypeToStyledString(fromType)} to {Question.TypeToStyledString(toType)}.";

			await context.RespondAsync(
				GenericEmbeds.Success("Set Bulk Question Types", body)
				);
			await Logging.Api.LogUserAction(context, config, "Set Bulk Question Types", message: body);
		}
    }
}
