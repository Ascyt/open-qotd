using DSharpPlus.Entities;

namespace OpenQotd.ActivitySwitcher
{
    /// <summary>
    /// Periodic timer that updates the bot's activity to a random preset.
    /// </summary>
    public class ActivitySwitcherTimer
    {
        private static readonly Random _random = new();
        private static string? _lastActivity = null;

        public static async Task ActivitySwitchLoopAsync(CancellationToken ct)
        {
            Console.WriteLine("Started activity switch loop.");
            while (true)
            {
                try
                {
                    await SwitchToRandomActivityAsync();
                    await UpdateTopGGServerCountAsync();
                    await UpdateDiscordForgeServerCountAsync();
                    await Task.Delay(Program.AppSettings.ActivitySwitchLoopDelayMs, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ActivitySwitchLoopAsync:\n{ex.Message}");
                }
            }
        }

        public static async Task SwitchToRandomActivityAsync()
        {
            int presetIndex = _random.Next(Presets.Values.Length);
            string activity = Presets.Values[presetIndex];

            if (activity == _lastActivity)
            {
                await SwitchToRandomActivityAsync(); // Try again if the same activity was chosen
                return;
            }
            _lastActivity = activity;

            await Program.Client.UpdateStatusAsync(new DiscordActivity(activity, DiscordActivityType.Custom));
            //await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Switched activity to: {activity}");
        }

        public static async Task UpdateTopGGServerCountAsync()
        {
            if (Program.AppSettings.BotId == 0)
                return;

            string? token = Environment.GetEnvironmentVariable("TOP_GG_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
                return;

            int guildCount = Program.Client.Guilds.Count;
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);

            StringContent content = new($"{{\"server_count\": {guildCount}}}", System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"https://top.gg/api/bots/{Program.AppSettings.BotId}/stats", content);
        }

        private static int _leftUntilNextForgeUpdate = 0; // Update the count every n activity switches to avoid hitting rate limits
        public static async Task UpdateDiscordForgeServerCountAsync()
        {
            string? token = Environment.GetEnvironmentVariable("DISCORD_FORGE_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
                return;

            if (_leftUntilNextForgeUpdate > 0)
            {
                _leftUntilNextForgeUpdate--;
                return;
            }
            _leftUntilNextForgeUpdate = (int)(1.0 / Program.AppSettings.ActivitySwitchLoopDelayMs * 1000 * 300); 


            int guildCount = Program.Client.Guilds.Count;
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);

            StringContent content = new($"{{\"server_count\": {guildCount}}}", System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync("https://discordforge.org/api/bots/stats", content);
        }
    }
}
