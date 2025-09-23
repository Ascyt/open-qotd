namespace OpenQotd.Bot.Database.Entities
{
    public class GuildUser
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; } 

        public int SelectedProfileId { get; set; } = 0;
    }
}
