namespace OpenQotd
{
    public class AppSettings
    {
        /// <summary>
        /// Whether or not to enable the database migration mode. If set, does not start the bot upon execution.
        /// </summary>
        /// <remarks>
        /// This is needed to apply database migrations, as for some reason .NET starts the program upon running any database migration command.
        /// </remarks>
        public bool EnableDbMigrationMode { get; set; }

        /// <summary>
        /// The current version of the bot, which vaguely follows <a href="https://semver.org/">Semantic Versioning</a>.
        /// </summary>
        public string Version { get; set; } = null!;

        /// <summary>
        /// The guild that any feedback (using /feedback or /presets suggest) gets sent to.
        /// </summary>
        public ulong FeedbackGuildId { get; set; }

        /// <summary>
        /// The channel within the feedback server provided with <see cref="FeedbackServerId" /> that any feedback gets sent to. 
        /// </summary>
        public ulong FeedbackChannelId { get; set; }

        /// <summary>
        /// Users that are blocked from suggesting feedback using /feedback or /presets suggest. 
        /// </summary>
        public HashSet<ulong> FeedbackBlockedUserIds { get; set; } = [];
    }
}
