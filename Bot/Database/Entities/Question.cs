using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OpenQotd.Bot.Database.Entities
{
    /// <summary>
    /// The type of a question.
    /// </summary>
    public enum QuestionType
    {
        /// <summary>
        /// A question that has been suggested by a user but not yet accepted.
        /// </summary>
        /// <remarks>
        /// Can be accepted/denied by users with the AdminRoleId or users with access to 
        /// the Accept/Deny buttons under suggestion messages.
        /// </remarks>
		Suggested = 0,

        /// <summary>
        /// A question that has been accepted by an admin and is eligible to be sent as QOTD.
        /// </summary>
        /// <remarks>
        /// Always takes priority over presets when sending QOTDs. Only questions of this type are sent as QOTD.
        /// </remarks>
        Accepted = 1,

        /// <summary>
        /// Represents the state of a message that has been successfully sent.
        /// </summary>
        /// <remarks>
        /// Users with the BasicRoleId can view all Sent questions, and they get used for the leaderboard and the `/topic` command.
        /// </remarks>
        Sent = 2,

        /// <summary>
        /// Represents a question that has been stashed away and will not be used for QOTDs unless manually changed back to Accepted.
        /// </summary>
        /// <remarks>
        /// If <see cref="Config.EnableDeletedToStash"/> is enabled, questions that are deleted are set to this type
        /// instead of being permanently deleted, unless they are already of this type.
        /// </remarks>
		Stashed = 3
	}

    public class Question
    {
        public int Id { get; set; }
        public int ConfigId { get; set; }

        /// <summary>
        /// For convenience, could otherwise be fetched from a join using <see cref="ConfigId"/>
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The ID of the question that is unique within a guild.
        /// </summary>
        /// <remarks>
        /// Used for referencing questions in commands, e.g. `/questions remove 5` or `/suggestions accept 3`.
        /// </remarks>
        public int GuildDependentId { get; set; }

        /// <summary>
        /// The type of the question, i.e. Suggested, Accepted, Sent, Stashed.
        /// </summary>
        public QuestionType Type { get; set; }

        /// <summary>
        /// The text contents of the question.
        /// </summary>
        [Required]
        public string? Text { get; set; }

        /// <summary>
        /// The user ID of the user who submitted or manually added the question.
        /// </summary>
        public ulong SubmittedByUserId { get; set; }

        /// <summary>
        /// Notes associated with the question, visible to users using a button when the question is sent as QOTD.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// The URL of a thumbnail image associated with the question. Will be fetched when the question is sent as QOTD.
        /// </summary>
        /// <remarks>
        /// Only image URLs from Discord or Imgur are allowed.
        /// </remarks>
        public string? ThumbnailImageUrl { get; set; }

        /// <summary>
        /// Additional information for staff, visible only to users with the <see cref="Config.AdminRoleId"/> or access to the suggestions channel when reviewing suggestions.
        /// </summary>
        public string? SuggesterAdminOnlyInfo { get; set; }

        /// <summary>
        /// The timestamp when the question was initially submitted or manually added.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The user ID of the user who accepted the question. Null if the question is not accepted or has been manually added.
        /// </summary>
        public ulong? AcceptedByUserId { get; set; }

        /// <summary>
        /// The timestamp when the question was accepted. Null if the question is not accepted or has been manually added.
        /// </summary>
        public DateTime? AcceptedTimestamp { get; set; }

        /// <summary>
        /// The timestamp when the question was last sent as QOTD. Null if the question has never been sent.
        /// </summary>
        public DateTime? SentTimestamp { get; set; }

        /// <summary>
        /// The value of the QOTD counter when the question was last sent. Null if the question has never been sent.
        /// </summary>
        public int? SentNumber { get; set; }

        /// <summary>
        /// The ID of the message in the suggestions channel that refers to this question. Null if not applicable.
        /// </summary>
        /// <remarks>
        /// Used to edit and unpin the message when the question is accepted/denied.
        /// </remarks>
        public ulong? SuggestionMessageId { get; set; }

        /// <summary>
        /// Gets the emoji associated with a given QuestionType.
        /// </summary>
        public static string GetEmoji(QuestionType type)
        {
			return type switch
			{
				QuestionType.Suggested => ":red_square:",
				QuestionType.Accepted => ":large_blue_diamond:",
				QuestionType.Sent => ":green_circle:",
				QuestionType.Stashed => ":heavy_multiplication_x:",
				_ => ":black_large_square:",
			};
		}
        /// <summary>
        /// Converts a QuestionType to a styled string with an emoji and markdown formatting.
        /// </summary>
        public static string TypeToStyledString(QuestionType type)
        {
            return $"{GetEmoji(type)} *{type}*";
        }

        public override string ToString()
            => ToString(longType: false);

        /// <param name="longType">If true, the type gets written out; otherwise, only an emoji is used.</param>
        public string ToString(bool longType)
        {
		    return longType ? 
                $"\"**{Text}**\" (Type: {TypeToStyledString(Type)}); by: <@{SubmittedByUserId}>; ID: `{GuildDependentId}`)" : 
                $"{GetEmoji(Type)} \"**{Text}**\" (by: <@{SubmittedByUserId}>; ID: `{GuildDependentId}`)";
		}
        /// <summary>
        /// Generates the next available GuildDependentId for a new question in the specified config.
        /// </summary>
        public static async Task<int> GetNextGuildDependentId(Config config)
        {
            using AppDbContext dbContext = new();
            try
            {
                return await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id)
                    .Select(q => q.GuildDependentId)
                    .MaxAsync() + 1;
            }
            catch (InvalidOperationException)
            {
                return 1; // No questions yet for this config
            }
        }

        /// <summary>
        /// Checks if the provided text is valid for a question, responding with an error message if not.
        /// </summary>
        /// <remarks>
        /// A question is considered valid if it is non-empty, does not exceed <see cref="AppSettings.QuestionTextMaxLength"/> characters,
        /// and does not contain any line-breaks. If the text is invalid and a <see cref="CommandContext"/> is provided,
        /// an appropriate error message is sent to the context.
        /// </remarks>
        public static async Task<bool> CheckTextValidity(string text, CommandContext? context, Config config, int? lineNumber=null)
        {
            string lineNumberString = lineNumber is null ? "" : $" (line {lineNumber})";

            if (string.IsNullOrWhiteSpace(text))
            {
                if (context is not null)
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Empty Question", message: $"Your question{lineNumberString} must not be empty."));
                return false;
            }

            if (text.Length > Program.AppSettings.QuestionTextMaxLength)
            {
                if (context is not null)
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Maximum Length Exceeded", message: $"Your question{lineNumberString} is {text.Length} characters in length, however it must not exceed **{Program.AppSettings.QuestionTextMaxLength}** characters."));
                return false;
            }

            if (text.Contains('\n'))
            {
                if (context is not null)
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Line-breaks are forbidden", message: $"Your question{lineNumberString} must not contain any line-breaks and must all be written in one line."));
                return false;
            }

            return true;
        }
    }
}
