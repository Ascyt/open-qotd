namespace OpenQotd.Bot.Exceptions
{
    /// <summary>
    /// Thrown when the bot's configuration has not been initialized but is required for an operation.
    /// </summary>
    public class ConfigNotInitializedException : BotException
    {
        public ConfigNotInitializedException() : base() { }
    }
}
