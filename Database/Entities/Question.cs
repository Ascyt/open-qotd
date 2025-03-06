﻿using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Database.Entities
{
    public enum QuestionType
    {
        Suggested, Accepted, Sent
    }

    public class Question
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

        public ulong? SuggestionMessageId { get; set; }

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
            List<int> existingIds;
            using (var dbContext = new AppDbContext())
            {
                existingIds = await dbContext.Questions
                    .Where(q => q.GuildId == guildId)
                    .Select(q => q.GuildDependentId)
                    .ToListAsync();
            }

            return existingIds.Max() + 1;
        }

        public static async Task<bool> CheckTextValidity(string text, CommandContext? context, int? lineNumber=null)
        {
            string lineNumberString = (lineNumber is null ? "" : $" (line {lineNumber})");

            if (text.Length > 256)
            {
                if (context is not null)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Maximum Length Exceeded", message: $"Your question{lineNumberString} is {text.Length} characters in length, however it must not exceed **256** characters."));
                return false;
            }

            if (text.Contains("\n"))
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
