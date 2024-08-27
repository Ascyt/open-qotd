using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Database.Entities
{
    public enum QuestionType
    {
        Suggested, Accepted, Sent
    }

    public class Question()
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public int GuildDependentId { get; set; }
        public QuestionType Type { get; set; }
        public string Text { get; set; }
        public ulong SubmittedByUserId { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong? AcceptedByUserId { get; set; }
        public DateTime? AcceptedTimestamp { get; set; }
        public DateTime? SentTimestamp { get; set; }
        public int? SentNumber { get; set; }

        public override string ToString()
        {
            string emoji;

            switch (Type)
            {
                case QuestionType.Suggested:
                    emoji = ":red_square:";
                    break;
                case QuestionType.Accepted:
                    emoji = ":large_blue_diamond:";
                    break;
                case QuestionType.Sent:
                    emoji = ":green_circle:";
                    break;
                default:
                    emoji = ":black_large_square:";
                    break;
            }

            return $"{emoji} \"**{Text}**\" (by: <@{SubmittedByUserId}>; ID: `{GuildDependentId}`)";
        }
        public static async Task<int> GetNextGuildDependentId(ulong guildId)
        {
            HashSet<int> existingIds;
            using (var dbContext = new AppDbContext())
            {
                existingIds = new HashSet<int>(await dbContext.Questions
                    .Where(q => q.GuildId == guildId)
                    .Select(q => q.GuildDependentId)
                    .ToListAsync());
            }

            int nextId = 1;
            while (existingIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId;
        }
    }
}
