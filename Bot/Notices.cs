using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenQotd.Bot
{
    /// <summary>
    /// Notices are messages that are written by developers and added under sent QOTDs as embeds.
    /// </summary>
    /// <remarks>
    /// They are stored in `notices.json`.
    /// </remarks>
    public static class Notices
    {
        /// <summary>
        /// Represents a notice message with a date, text and importance flag.
        /// </summary>
        /// <remarks>
        /// See also: <seealso cref="QotdSending.QotdSender.SendNoticeIfAvailable(QotdSending.SendQotdData)"/>
        /// </remarks>
        public class Notice
        {
            /// <summary>
            /// Represents the date when the notice becomes active and visible to users.
            /// </summary>
            /// <remarks>
            /// A notice will be sent if its date is between the last sent QOTD and the current time,
            /// alongside other requirements.
            /// </remarks>
            [JsonPropertyName("date")]
            public required DateTime Date { get; set; }

            /// <summary>
            /// The text of the notice to be displayed.
            /// </summary>
            [JsonPropertyName("notice")]
            public required string NoticeText { get; set; }

            /// <summary>
            /// Notices marked as important are sent if the notice level is either Important or All, and others only get sent if the notice level is All.
            /// </summary>
            [JsonPropertyName("important")]
            public bool IsImportant { get; set; }
        }

        private static JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        private static List<Notice>? _notices = null;
        /// <summary>
        /// Get all notices, loading them from notices.json if not already loaded.
        /// </summary>
        public static List<Notice> notices
        {
            get
            {
                if (_notices is null)
                    LoadNoticesAsync().Wait();

                return _notices!;
            }
        }

        /// <summary>
        /// Load notices from notices.json, or create an empty list if the file does not exist.
        /// </summary>
        public static async Task LoadNoticesAsync()
        {
            if (!File.Exists("notices.json"))
            {
                _notices = [];
                await SaveNoticesAsync();
                return;
            }

            string jsonData = await File.ReadAllTextAsync("notices.json");

            _notices = JsonSerializer.Deserialize<List<Notice>>(jsonData);
        }
        /// <summary>
        /// Save the current notices to notices.json.
        /// </summary>
        public static async Task SaveNoticesAsync()
        {
            string jsonData = JsonSerializer.Serialize(notices, _serializerOptions);

            await File.WriteAllTextAsync("notices.json", jsonData);
        }

        /// <summary>
        /// Gets the latest notice that is not in the UTC future.
        /// </summary>
        public static Notice? GetLatestAvailableNotice()
        {
            return notices
                .Where(n => n.Date <= DateTime.UtcNow)
                .MaxBy(n => n.Date);
        }
    }
}
