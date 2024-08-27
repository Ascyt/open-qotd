using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;

namespace CustomQotd.Features.Commands
{
    [Command("questions")]
    public class QuestionsCommand
    {
        [Command("add")]
        public static async Task AddQuestionAsync(CommandContext context,
            [System.ComponentModel.Description("The question to add.")] string question,
            [System.ComponentModel.Description("The type of the question to add.")] QuestionType type)
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
    }
}
