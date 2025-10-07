using DSharpPlus.Entities;

namespace OpenQotd.Bot.ActivitySwitcher
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
                    await Task.Delay(Program.AppSettings.ActivitySwitchLoopDelayMs, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FetchLoopAsync:\n{ex.Message}");
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
    }
}
