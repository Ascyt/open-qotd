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
            [JsonPropertyName("important")]
            public bool IsImportant { get; set; }
        }

        private static List<Notice>? _notices = null;
        public static List<Notice> notices
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

            _notices = JsonSerializer.Deserialize<List<Notice>>(jsonData);
        }
        public static async Task SaveNotices()
        {
            string jsonData = JsonSerializer.Serialize<List<Notice>>(notices, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync("notices.json", jsonData);
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
