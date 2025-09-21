namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// General exception for errors that occur during the QOTD sending process.
    /// </summary>
    public abstract class QotdSenderException : Exception
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
