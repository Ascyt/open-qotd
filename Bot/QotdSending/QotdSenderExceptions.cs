namespace CustomQotd.Bot.QotdSending
{
    public abstract class QotdSenderException : Exception
    {
        public QotdSenderException() : base() { }
        public QotdSenderException(string message) : base(message) { }
        public QotdSenderException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class QotdChannelNotFoundException : QotdSenderException
    {
        public QotdChannelNotFoundException() : base() { }
    }
}
