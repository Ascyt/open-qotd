namespace OpenQotd
{
    public class AppSettings
    {

        /// <summary>
        /// The current version of the bot, which vaguely follows <a href="https://semver.org/">Semantic Versioning</a>.
        /// </summary>
        public string Version { get; set; } = "0.0.0";

        /// <summary>
        /// Whether or not to enable the database migration mode. If set, does not start the bot upon execution.
        /// </summary>
        /// <remarks>
        /// This is needed to apply database migrations, as for some reason .NET starts the program upon running any database migration command.
        /// </remarks>
        public bool EnableDbMigrationMode { get; set; } = false;

        /// <summary>
        /// The postgres database connection string to use, excluding its password. 
        /// </summary>
        /// <remarks>
        /// The password should be provided in the environment variable "POSTGRES_PASSWORD" instead. 
        /// </remarks>
        public string PostgresConnectionStringNoPassword { get; set; } = null!;

        /// <summary>
        /// The minimum delay between continuous checks for new QOTDs that are to be sent. 
        /// </summary>
        public int QotdSendingFetchLoopDelayMs { get; set; } = 100;

        /// <summary>
        /// The maximum amount of parallel threads to be used for sending QOTDs.
        /// </summary>
        public int QotdSendingMaxDegreeOfParallelism { get; set; } = 10;

        /// <summary>
        /// The maximum amount of questions a guild can have. Does not add a database constraint. 
        /// </summary>
        public int QuestionsPerGuildMaxAmount { get; set; } = 65536;

        /// <summary>
        /// The maximum amount of characters that Config.QotdTitle can be. Does not add a database constraint.
        /// </summary>
        /// <remarks>
        /// Changing this to a value greater than 66 is likely to cause issues upon sending QOTDs due to Discord's length limit on buttons. 
        /// </remarks>
        public int ConfigQotdTitleMaxLength = 64;

        /// <summary>
        /// The maximum amount of characters that Question.Text can be. Does not add a database constraint.
        /// </summary>
        public int QuestionTextMaxLength { get; set; } = 256;

        /// <summary>
        /// The amount of items to show per page for list messages. 
        /// </summary>
        public int ListMessageItemsPerPage { get; set; } = 10;

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

        /// <summary>
        /// Users that are allowed to run /debug.
        /// </summary>
        /// <remarks>
        /// /debug is a powerful and dangerous command that, amongst other things, 
        /// gives the user read-and-write access to the entire database, so make sure to exercise caution. 
        /// </remarks>
        public HashSet<ulong> DebugAllowedUserIds { get; set; } = [];
    }
}
