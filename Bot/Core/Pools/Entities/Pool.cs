using System.ComponentModel.DataAnnotations.Schema;
using OpenQotd.Core.Configs.Entities;

namespace OpenQotd.Core.Pools.Entities
{
    public sealed class Pool
    {
        public enum OrderingOption
        {
            Random = 0, 
            Queue = 1, 
            ReverseQueue = 2
        }
        
        /// <summary>
        /// What to do with a question after it has been sent as a QOTD.
        /// </summary>
        public enum AlterQuestionAfterSentOption
        {
            /// <summary>
            /// The question gets the Sent type.
            /// </summary>
            QuestionToSent = 0,
            /// <summary>
            /// The question gets the Sent type, and if there are no more Accepted questions, all Sent questions are reset to Accepted.
            /// </summary>
            QuestionToSentAndResetIfEmpty = 1,
            /// <summary>
            /// The question remains Accepted and can thus be sent again in the future.
            /// </summary>
            QuestionStaysAccepted = 2,
            /// <summary>
            /// The question gets the Suggested type again.
            /// </summary>
            QuestionToSuggested = 3,
            /// <summary>
            /// The question gets the Stashed type if <see cref="EnableDeletedToStash"/> is true, otherwise it is permanently deleted.
            /// </summary>
            RemoveQuestion = 4
        }

        public int Id { get; set; }
        
        public ICollection<Config>? Configs { get; set; }

        public ICollection<PoolItem>? PoolItems { get; set; }

        /// <summary>
        /// The name of the pool that will be shown to users.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the pool is enabled or disabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether new questions should be added to this pool by default.
        /// </summary>
        /// <remarks>
        /// If multiple pools have this option enabled, the question will be added to all of them.
        /// </remarks>
        public bool NewQuestionsToThis { get; set; } = false;

        /// <summary>
        /// Next to AdminRole, the ModRole can also manage the pool, but cannot change its settings or delete it.
        /// </summary>
        public ulong? ModRoleId { get; set; } = null;

        /// <summary>
        /// The ordering option for selecting items from the pool.
        /// </summary>
        public OrderingOption Ordering { get; set; } = OrderingOption.Random;

        /// <summary>
        /// Whether to delete the pool when it becomes empty.
        /// </summary>
        public bool DeleteOnEmpty { get; set; } = true;

        /// <summary>
        /// Whether to exclude questions that are in this pool from being selected for normal QOTD sending.
        /// </summary>
        public bool ExcludeQuestionsForNormal { get; set; } = false;

        /// <summary>
        /// What to do with a question after it has been sent as a QOTD.
        /// </summary>
        public AlterQuestionAfterSentOption QotdAlterQuestionAfterSent { get; set; } = AlterQuestionAfterSentOption.QuestionToSent;

        /// <summary>
        /// If true, the pool acts as if it were to not exist when it is disabled.
        /// </summary>
        public bool PassthroughOnDisabled { get; set; } = true;

        /// <summary>
        /// If true, the pool acts as if it were to not exist when there is no Accepted question in it. 
        /// </summary>
        public bool PassthroughOnEmpty { get; set; } = true;

        /// <summary>
        /// The probability that the pool will be skipped when attempting to send a question.
        /// </summary>
        public double PassthroughProbability { get; set; } = 0.0;
        
        /// <summary>
        /// If not null, the date when the pool will be enabled automatically.
        /// </summary>
        /// <remarks>
        /// The pool should be enabled at that day before the QOTD is sent.
        /// </remarks>
        public DateTime? ScheduleEnableDate { get; set; }

        /// <summary>
        /// If not null, the date when the pool will be disabled automatically.
        /// </summary>
        /// <remarks>
        /// The pool should be disabled at that day before the QOTD is sent.
        /// </remarks>
        public DateTime? ScheduleDisableDate { get; set; }

        // Internal Variables

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"{Name}{(Enabled ? "" : " (disabled)")}";
        }
    }
}
