namespace OpenQotd
{
    public class AppSettings
    {
        public string ApiKey { get; set; }
        public bool IsFeatureEnabled { get; set; }
        public int MaxRetries { get; set; }
        public int TimeoutSeconds { get; set; }
    }
}
