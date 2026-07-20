using System.ComponentModel.DataAnnotations.Schema;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Pools.Entities
{
    public sealed class PoolItem
    {
        /// <summary>
        /// The type of the entry.
        /// </summary>
        public enum ItemType
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

        public int Id { get; set; }

        [ForeignKey("Pool")]
        public int PoolId { get; set; }
        public Pool? Pool { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        /// <summary>
        /// The type of the question, i.e. Suggested, Accepted, Sent, Stashed.
        /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// Is used for Queue and ReverseQueue ordering to track the position of the question in the pool.
        /// </summary>
        /// <remarks>
        /// Initially set to question's GuildDependentId * 2^24. Make sure to avoid midpoint issues when inserting between entries.
        /// </remarks>
        public long OrderInPool { get; set; }

                /// <summary>
        /// Gets the emoji associated with a given EntryType.
        /// </summary>
        public static string GetEmoji(ItemType type)
        {
            return type switch
            {
                ItemType.Suggested => ":red_square:",
                ItemType.Accepted => ":large_blue_diamond:",
                ItemType.Sent => ":green_circle:",
                ItemType.Stashed => ":heavy_multiplication_x:",
                _ => ":black_large_square:",
            };
        }
        /// <summary>
        /// Converts a EntryType to a styled string with an emoji and markdown formatting.
        /// </summary>
        public static string TypeToStyledString(ItemType type)
        {
            return $"{GetEmoji(type)} *{type}*";
        }

        public override string ToString()
            => ToString(longVersion: false);

        /// <param name="longVersion">If true, the type and full question gets written out; otherwise, the question is shortened to a single line and only an emoji is used.</param>
        public string ToString(bool longVersion)
        {
            // TODO: Ensure question is loaded. 

            return longVersion ?
                $"\"{Helpers.General.Italicize(Question.Text!)}\" (Type: {TypeToStyledString(Type)}); by: <@{Question.SubmittedByUserId}>; ID: `{Question.GuildDependentId}`)" :
                $"{GetEmoji(Type)} \"*{Helpers.General.TrimIfNecessary(Question.Text!, 64)}*\" (by: <@{Question.SubmittedByUserId}>; ID: `{Question.GuildDependentId}`)";
        }
    }
}
