using DSharpPlus.Commands;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Questions
{
    internal static class Api
    {
        /// <summary>
        /// Check if adding <see cref="additionalAmount"/> questions would exceed the maximum allowed amount of questions per guild.
        /// </summary>
        /// <remarks>
        /// The maximum amount is given by <see cref="AppSettings.QuestionsPerGuildMaxAmount"/>.
        /// </remarks>
        public static async Task<bool> IsWithinMaxQuestionsAmount(CommandContext context, int additionalAmount)
        {
            if (additionalAmount < 0)
                return false;

            using AppDbContext dbContext = new();

            int currentAmount = dbContext.Questions
                .Where(q => q.GuildId == context.Guild!.Id)
                .Count();

            bool isWithinLimit = currentAmount + additionalAmount <= Program.AppSettings.QuestionsPerGuildMaxAmount;

            if (!isWithinLimit)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(
                        $"It is not allowed to have more than **{Program.AppSettings.QuestionsPerGuildMaxAmount}** questions in a guild. " +
                        $"There are currently {currentAmount} questions, and adding {additionalAmount} more would exceed the limit, therefore no questions have been added."));
            }

            return isWithinLimit;
        }
    }
}
