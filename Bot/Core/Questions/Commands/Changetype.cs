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
        [Command("changetype")]
        [Description("Change the type of a question (eg. Sent->Accepted).")]
        public static async Task ChangeTypeOfQuestionAsync(CommandContext context,
            [Description("The ID of the question.")] int questionId,
            [Description("The type to set the question to.")] QuestionType type)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            ulong guildId = context.Guild!.Id;

            Question? question;
            string body;
            QuestionType fromType;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Question Not Found", message: $"The question with ID `{questionId}` could not be found."));
                    return;
                }
                fromType = question.Type;
                body = $"\n> {Question.TypeToStyledString(fromType)} → {Question.TypeToStyledString(type)}";

                question.Type = type;

                body = question.ToString() + body;

                await dbContext.SaveChangesAsync();
            }

            if (fromType == QuestionType.Suggested && type != QuestionType.Suggested)
            {
                // Set suggestion message to modified state
                await Suggestions.Helpers.General.TrySetSuggestionMessageToModifiedIfEnabledAsync(question, config, context.Guild!);
            }
            else if (fromType != QuestionType.Suggested && type == QuestionType.Suggested)
            {
                // Send suggestion notification message
                await Suggestions.Helpers.General.TryResetSuggestionMessageIfEnabledAsync(question, config, context.Guild!);
            }

            await context.RespondAsync(
                GenericEmbeds.Success("Set Question Type", body)
                );
            await Logging.Api.LogUserActionAsync(context, config, "Set Question Type", message: body);
		}
    }
}
