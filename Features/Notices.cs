using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomQotd.Features
{
    public static class Notices
    {
        public class Notice
        {
            [JsonPropertyName("date")]
            public required DateTime Date { get; set; }

            [JsonPropertyName("notice")]
            public required string NoticeText { get; set; }
        }

        private static Notice[]? _notices = null;
        public static Notice[] notices
        {
            get
            {
                if (_notices is null)
                    LoadNotices().Wait();

                return _notices!;
            }
        }

        public static async Task LoadNotices()
        {
            string jsonData = await File.ReadAllTextAsync("notices.json");

            _notices = JsonSerializer.Deserialize<Notice[]>(jsonData);
        }

        /// <summary>
        /// Gets the latest notice that is not in the UTC future
        /// </summary>
        public static Notice? GetLatestAvailableNotice()
        {
            return notices.Where(n => n.Date <= DateTime.UtcNow).FirstOrDefault();
        }
    }
}
