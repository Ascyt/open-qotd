using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Exceptions;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.QotdSending
{
    /// <summary>
    /// Periodic timer that checks which guilds need to be sent a QOTD and sends them.
    /// </summary>
    public class QotdSenderTimer
    {
        /// <summary>
        /// Sends all available QOTDs, maximum <see cref="AppSettings.QotdSendingMaxDegreeOfParallelism"/> at a time.
        /// </summary>
        public static async Task SendQotdsAsync()
        {
            int currentDay = DateTime.UtcNow.Day;
            int currentHour = DateTime.UtcNow.Hour;
            int currentMinute = DateTime.UtcNow.Minute;

            Config[] configs;
            using (AppDbContext dbContext = new())
            {
                configs = await dbContext.Configs
                    .Where(c => c.EnableAutomaticQotd && 
                        (c.LastSentTimestamp == null ||
                        c.LastSentTimestamp.Value.Day != currentDay) && 
                        ((currentHour == c.QotdTimeHourUtc && currentMinute >= c.QotdTimeMinuteUtc) || currentHour > c.QotdTimeHourUtc))
                    .ToArrayAsync();
            }

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
