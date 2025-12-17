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
		[Command("remove")]
        [Description("Remove a question to stash or irreversibly delete it if disabled.")]
        public static async Task RemoveQuestionAsync(CommandContext context,
        [Description("The ID of the question.")] int questionId)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.UserIsAdmin(context, config))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            QuestionType originalType;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title:"Question Not Found", message:$"The question with ID `{questionId}` could not be found."));
                    return;
                }
                originalType = question.Type;

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

            if (originalType == QuestionType.Suggested)
            {
                // Set suggestion message to modified state
                await Helpers.Suggestions.General.TrySetSuggestionMessageToModifiedIfEnabledAsync(question, config, context.Guild!);
            }

            string title = config.EnableDeletedToStash && originalType != QuestionType.Stashed ? "Removed Question to Stash" : "Removed Question";

			await context.RespondAsync(
                GenericEmbeds.Success(title, body)
                );
            await Logging.Api.LogUserAction(context, config, title, body);
        }
    }
}
