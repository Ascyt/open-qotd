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
        [Command("clearstash")]
        [Description("Remove all questions of Stashed type.")]
        public static async Task ClearStashAsync(CommandContext context)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            ulong guildId = context.Guild!.Id;

			List<Question>? questions;
			using (AppDbContext dbContext = new())
			{
				questions = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Stashed)
                    .ToListAsync();

				dbContext.Questions.RemoveRange(questions);
				await dbContext.SaveChangesAsync();
			}
			string body = $"Removed {questions.Count} question{(questions.Count == 1 ? "" : "s")} of type {Question.TypeToStyledString(QuestionType.Stashed)}.";

			string title = "Cleared Stash";

			await context.RespondAsync(
				GenericEmbeds.Success(title, body)
				);
			await Logging.Api.LogUserActionAsync(context, config, title, body);
		}
    }
}
