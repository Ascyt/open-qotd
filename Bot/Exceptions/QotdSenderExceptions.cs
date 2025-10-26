namespace OpenQotd.Exceptions
{
    /// <summary>
    /// General exception for errors that occur during the QOTD sending process.
    /// </summary>
    public abstract class QotdSenderException : BotException
    {
        public QotdSenderException() : base() { }
        public QotdSenderException(string message) : base(message) { }
        public QotdSenderException(string message, Exception innerException) : base(message, innerException) { }
    }
    /// <summary>
    /// Thrown when the QOTD channel configured for a guild cannot be found.
    /// </summary>
    public class QotdChannelNotFoundException : QotdSenderException
    {
        public QotdChannelNotFoundException() : base() { }
    }
}
