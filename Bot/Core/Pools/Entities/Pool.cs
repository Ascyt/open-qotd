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

        public int Id { get; set; }
        [ForeignKey("Config")]
        public int ConfigId { get; set; }
        public Config? Config { get; set; }

        public ICollection<PoolEntry>? PoolEntries { get; set; }

        /// <summary>
        /// The name of the pool that will be shown to users.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the pool is enabled or disabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The ordering option for selecting items from the pool.
        /// </summary>
        public OrderingOption Ordering { get; set; } = OrderingOption.Random;

        /// <summary>
        /// If not null, the date when the pool will be enabled automatically.
        /// </summary>
        /// <remarks>
        /// The pool should be enabled at that day before the QOTD is sent.
        /// </remarks>
        public DateTime? ScheduleEnable { get; set; }

        /// <summary>
        /// If not null, the date when the pool will be disabled automatically.
        /// </summary>
        /// <remarks>
        /// The pool should be disabled at that day before the QOTD is sent.
        /// </remarks>
        public DateTime? ScheduleDisable { get; set; }

        /// <summary>
        /// Whether to delete the pool when it becomes empty.
        /// </summary>
        public bool DeleteOnEmpty { get; set; } = true;

        /// <summary>
        /// Whether to exclude questions that are in this pool from being selected for normal QOTD sending.
        /// </summary>
        public bool ExcludeQuestionsForNormal { get; set; } = false;

        /// <summary>
        /// Override for QotdAlterQuestionAfterSent variable specifically for that pool.
        /// </summary>
        public Config.AlterQuestionAfterSentOption? OverrideAlterQuestionAfterSent { get; set; }

        /// <summary>
        /// Next to AdminRole, the ModRole can also manage the pool, but cannot change its settings or delete it.
        /// </summary>
        public ulong? ModRoleId { get; set; } = null; 

        // Internal Variables

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"{Name} ";
        }
    }
}
