namespace CustomQotd.Database
{
    public class DatabaseException(string message) : System.Exception
    {
        public new string Message { get; set; } = message;
    }
}
