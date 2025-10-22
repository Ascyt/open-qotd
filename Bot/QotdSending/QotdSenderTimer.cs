using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Exceptions;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// Periodic timer that checks which for configs need to be sent a QOTD and sends them.
    /// </summary>
    public class QotdSenderTimer
    {
        /// <summary>
        /// Stores config IDs which are to be sent as QOTD at the given hour and minute.
        /// </summary>
        /// <remarks>
        /// Array of hour, minute (length 1440). Null if not initialized yet.
        /// </remarks>
        private static HashSet<int>[][]? _configIdsToSendQotdToday = null;

        /// <summary>
        /// Stores config IDs which are to be ignored today for sending QOTDs because they have sending disabled or the day condition does not match.
        /// </summary>
        private static HashSet<int>? _ignoredConfigIdsToday = null;

        /// <summary>
        /// The current index in <see cref="_configIdsToSendQotdToday"/> that is being processed.
        /// </summary>
        /// <remarks>
        /// Should ideally be equal to the current time's hour, minute. Is between 0 and 1439.
        /// </remarks>
        private static int _currentIndex = -1;

        /// <summary>
        /// (Re-)loads all configs from the database and populates <see cref="_configIdsToSendQotdToday"/> and <see cref="_ignoredConfigIdsToday"/>.
        /// </summary>
        /// <returns></returns>
        public static async Task LoadAllAsync()
        {
            HashSet<ConfigToSendElement> allWithSendingEnabled = GetAllConfigsWithSendingEnabled();

            foreach (ConfigToSendElement element in allWithSendingEnabled)
            {
            }
        }

        private struct ConfigToSendElement
        {
            public int ConfigId;
            public int Hour;
            public int Minute;
            public string? DayCondition;
            public DateTime? DayConditionLastChanged;
        }
        private static HashSet<ConfigToSendElement> GetAllConfigsWithSendingEnabled()
        {
            using AppDbContext dbContext = new();

            return [.. dbContext.Configs
                    .Where(c => c.EnableAutomaticQotd)
                    .Select(c => new ConfigToSendElement { 
                        ConfigId = c.Id, 
                        Hour = c.QotdTimeHourUtc, 
                        Minute = c.QotdTimeMinuteUtc, 
                        DayCondition = c.QotdTimeDayCondition, 
                        DayConditionLastChanged = c.QotdTimeDayConditionLastChangedTimestamp } )];
        }

        /// <summary>
        /// Send QOTDs to all configs that need one right now.
        /// </summary>
        public static async Task SendQotdsAsync()
        {
            int requiredIndex = DateTime.UtcNow.Hour * 60 + DateTime.UtcNow.Minute;
        }

        public static async Task SendQotdsForIndexAsync(int index)
        {
            await SendQotdsForConfigsAsync(configsToSend);
        }

        /// <summary>
        /// Send QOTDs for the given configs, maximum <see cref="AppSettings.QotdSendingMaxDegreeOfParallelism"/> at a time.
        /// </summary>
        public static async Task SendQotdsForConfigsAsync(Config[] configs)
        {
            if (configs.Length == 0)
                return;

            Notices.Notice? latestAvailableNotice = Notices.GetLatestAvailableNotice();

            // Send QOTDs in parallel, but limit the degree of parallelism to avoid overwhelming the database or Discord API
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = Program.AppSettings.QotdSendingMaxDegreeOfParallelism
            };
            await Parallel.ForEachAsync(configs, options, async (config, ct) =>
            {
                await SendNextQotdIgnoreExceptions(config, latestAvailableNotice);
            });

            await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Sent {configs.Length}");
        }

        /// <summary>
        /// Send the next QOTD for the guild and catch and ignore/print all exceptions.
        /// </summary>
        private static async Task SendNextQotdIgnoreExceptions(Config config, Notices.Notice? latestAvailableNotice)
        {
            try
            {
                await QotdSender.FetchGuildAndSendNextQotdAsync(config, latestAvailableNotice);
            }
            catch (QotdChannelNotFoundException)
            {
                // This exception is expected if the QOTD channel is not set for the guild.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending QOTD for config {config.Id} (guild {config.GuildId}): {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Continuously filters by available QOTDs to send every <see cref="AppSettings.QotdSendingFetchLoopDelayMs"/> milliseconds.
        /// </summary>
        /// <remarks>
        /// Another check will not occur unless all QOTDs of the previous one have been sent.
        /// </remarks>
        public static async Task FetchLoopAsync(CancellationToken ct)
        {
            Console.WriteLine("Started fetch loop.");
            while (true)
            {
                try
                {
                    await Task.Delay(Program.AppSettings.QotdSendingFetchLoopDelayMs, ct);
                    //await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Check time");
                    await SendQotdsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FetchLoopAsync:\n{ex.Message}");
                }
            }
        }
    }
}
