namespace OpenQotd.Core.Exceptions
{
    /// <summary>
    /// The base exception type for all bot-related exceptions.
    /// </summary>
    public class BotException : Exception
    {
        public BotException() : base() { }
        public BotException(string message) : base(message) { }
        public BotException(string message, Exception innerException) : base(message, innerException) { }
    }
}
