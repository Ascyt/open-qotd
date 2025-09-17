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
        public ulong GuildId { get; set; }
        public int PresetIndex { get; set; }
    }
}
