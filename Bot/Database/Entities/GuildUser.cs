namespace OpenQotd.Bot.Database.Entities
{
    public class GuildUser
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }

        /// <summary>
        /// The selected profile ID for this user in this guild.
        /// </summary>
        public int SelectedProfileId { get; set; } = 0;
    }
}
