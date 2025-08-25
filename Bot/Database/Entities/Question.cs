using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OpenQotd.Bot.Database.Entities
{
    public enum QuestionType
    {
		Suggested = 0,
        Accepted = 1, 
        Sent = 2,
		Stashed = 3,
	}

    public class Question
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public int GuildDependentId { get; set; }
        public QuestionType Type { get; set; }
        [Required]
        [MaxLength(256)]
        public string? Text { get; set; }
        public ulong SubmittedByUserId { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong? AcceptedByUserId { get; set; }
        public DateTime? AcceptedTimestamp { get; set; }
        public DateTime? SentTimestamp { get; set; }
        public int? SentNumber { get; set; }

        public ulong? SuggestionMessageId { get; set; }

        public override string ToString()
        {
            string emoji = Type switch
            {
                QuestionType.Suggested => ":red_square:",
                QuestionType.Accepted => ":large_blue_diamond:",
                QuestionType.Sent => ":green_circle:",
                QuestionType.Stashed => ":heavy_multiplication_x:",
                _ => ":black_large_square:",
            };
            return $"{emoji} \"**{Text}**\" (by: <@{SubmittedByUserId}>; ID: `{GuildDependentId}`)";
        }
        public static async Task<int> GetNextGuildDependentId(ulong guildId)
        {
            List<int> existingIds;
            using (var dbContext = new AppDbContext())
            {
                existingIds = await dbContext.Questions
                    .Where(q => q.GuildId == guildId)
                    .Select(q => q.GuildDependentId)
                    .ToListAsync();
            }

            return existingIds.Count == 0 ? 1 : existingIds.Max() + 1;
        }

        const int MAX_LENGTH = 256;
        public static async Task<bool> CheckTextValidity(string text, CommandContext? context, Config config, int? lineNumber=null)
        {
            string lineNumberString = lineNumber is null ? "" : $" (line {lineNumber})";

            if (string.IsNullOrWhiteSpace(text))
            {
                if (context is not null)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Empty Question", message: $"Your question{lineNumberString} must not be empty."));
                return false;
            }

            if (text.Length > MAX_LENGTH)
            {
                if (context is not null)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Maximum Length Exceeded", message: $"Your question{lineNumberString} is {text.Length} characters in length, however it must not exceed **{MAX_LENGTH}** characters."));
                return false;
            }

            if (text.Contains('\n'))
            {
                if (context is not null)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Line-breaks are forbidden", message: $"Your question{lineNumberString} must not contain any line-breaks and must all be written in one line."));
                return false;
            }

            return true;
        }
    }
}
