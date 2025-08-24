using CustomQotd.Bot.Database;
using CustomQotd.Bot.Database.Entities;
using CustomQotd.Bot.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CustomQotd.Bot.Commands
{
    public class LeaderboardCommand
    {
        private class LeaderboardEntry
        {
            public ulong UserId { get; set; }
            public int Count { get; set; }

            public override string ToString()
                => $"%index%. <@!{UserId}>: **{Count}**";
        }
        [Command("lb")]
        [Description("View a leaderboard of who wrote the most sent QOTDs.")]
        public static async Task LeaderboardShorthandAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1)
            => await LeaderboardAsync(context, page);

        [Command("leaderboard")]
        [Description("View a leaderboard of who wrote the most sent QOTDs.")]
        public static async Task LeaderboardAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.UserIsBasic(context, null))
                return;

            List<Question> sentQuestions;
            using (var dbContext = new AppDbContext())
            {
                sentQuestions = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.Type == QuestionType.Sent)
                    .ToListAsync();
            }

            Dictionary<ulong, int> entries = [];

            foreach (var question in sentQuestions)
            {
                if (entries.ContainsKey(question.SubmittedByUserId))
                {
                    entries[question.SubmittedByUserId]++;
                }
                else
                {
                    entries.Add(question.SubmittedByUserId, 1);
                }
            }

            List<LeaderboardEntry> sortedEntries = [.. entries
                .OrderByDescending(pair => pair.Value)
                .Select(pair => new LeaderboardEntry() { UserId = pair.Key, Count = pair.Value })];

            const int itemsPerPage = 10;
            await MessageHelpers.ListMessageComplete(context, page, "QOTD Leaderboard",
                (int page) =>
                {
                    LeaderboardEntry[] filteredEntries = [.. sortedEntries
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    int totalEntries = sortedEntries.Count;

                    int totalPages = (int)Math.Ceiling(totalEntries / (double)itemsPerPage);

                    return Task.FromResult<(LeaderboardEntry[], int, int, int)>((filteredEntries, totalEntries, totalPages, itemsPerPage));
                });
        }
    }
}
