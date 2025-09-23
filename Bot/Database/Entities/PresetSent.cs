namespace OpenQotd.Bot.Database.Entities
{
    /// <summary>
    /// Represents a record of a preset question that has been sent to a guild.
    /// </summary>
    /// <remarks>
    /// Used to track which preset questions have already been sent to avoid repetition.
    /// </remarks>
    public class PresetSent
    {
        public int Id { get; set; }
        public int ConfigIdx { get; set; }

        /// <summary>
        /// For convenience, could otherwise be fetched from a join using <see cref="ConfigId"/>
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The line number of the preset within presets.txt.
        /// </summary>
        public int PresetIndex { get; set; }
    }
}
