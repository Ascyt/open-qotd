using DSharpPlus.Entities;

namespace OpenQotd.Core.ActivitySwitcher
{
    /// <summary>
    /// Periodic timer that updates the bot's activity to a random preset.
    /// </summary>
    public class Timer
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
            int presetIndex = _random.Next(Presets.Api.Presets.Length);
            string activity = Presets.Api.Presets[presetIndex];

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
    }
}
