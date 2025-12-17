using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQotd.Database.Entities
{
    public class PoolEntry
    {
        public int Id { get; set; }

        [ForeignKey("Pool")]
        public int PoolId { get; set; }
        public Pool? Pool { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        /// <summary>
        /// Is used for Queue and ReverseQueue ordering to track the position of the question in the pool.
        /// </summary>
        /// <remarks>
        /// Initially set to question's GuildDependentId * 2^24. Make sure to avoid midpoint issues when inserting between entries.
        /// </remarks>
        public long OrderInPool { get; set; }
    }
}
