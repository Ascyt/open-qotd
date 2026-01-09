using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.UncategorizedCommands
{
    public sealed class LeaderboardCommand
    {
        private class LeaderboardEntry
        {
            public ulong UserId { get; set; }
            public int Count { get; set; }

            public override string ToString()
                => $"<@!{UserId}>: **{Count}**";
        }
        [Command("lb")]
        [Description("View a leaderboard of who wrote the most sent QOTDs.")]
        public static async Task LeaderboardShorthandAsync(CommandContext context,
            [Description("Which OpenQOTD profile to consider.")][SlashAutoCompleteProvider<Profiles.AutoCompleteProviders.ViewableProfiles>] int of,
            [Description("The page of the listing (default 1).")] int page = 1)
            => await LeaderboardAsync(context, of, page);

        [Command("leaderboard")]
        [Description("View a leaderboard of who wrote the most sent QOTDs.")]
        public static async Task LeaderboardAsync(CommandContext context,
            [Description("Which OpenQOTD profile to consider.")][SlashAutoCompleteProvider<Profiles.AutoCompleteProviders.ViewableProfiles>] int of,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            int profileId = of;

            Config? config = await Profiles.Api.TryGetConfigAsync(context, of);
            if (config is null || !await Permissions.Api.Basic.CheckAsync(context, config))
                return;

            List<Question> sentQuestions;
            using (AppDbContext dbContext = new())
            {
                sentQuestions = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Sent)
                    .ToListAsync();
            }

            Dictionary<ulong, int> entries = [];

            foreach (Question question in sentQuestions)
            {
                if (entries.TryGetValue(question.SubmittedByUserId, out int value))
                {
                    entries[question.SubmittedByUserId] = ++value;
                }
                else
                {
                    entries.Add(question.SubmittedByUserId, 1);
                }
            }

            Random _random = new();

            List<LeaderboardEntry> sortedEntries = [.. entries
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => (_random.Next() % 2 == 0) ? -1 : 1) // Randomize order of users with same count
                .Select(pair => new LeaderboardEntry() { UserId = pair.Key, Count = pair.Value })];

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNewAsync(context, page, "QOTD Leaderboard",
                (int page) =>
                {
                    LeaderboardEntry[] filteredEntries = [.. sortedEntries
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    int totalEntries = sortedEntries.Count;

                    PageInfo<LeaderboardEntry> pageInfo = new()
                    {
                        Elements = filteredEntries,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalEntries
                    };

                    return Task.FromResult(pageInfo);
                }, ListLeaderboardEntryToString);
        }

        private static string ListLeaderboardEntryToString(LeaderboardEntry leaderboardEntry, int rank)
        {
            string rankEmoji = rank switch
            {
                1 => ":yellow_circle:",
                2 => ":white_circle:",
                3 => ":brown_circle:",
                _ => $":black_small_square:"
            };

            return $"{rankEmoji} **#{rank}** - {leaderboardEntry}";
        }
    }
}
