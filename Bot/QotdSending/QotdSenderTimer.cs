using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;

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

            ulong[] guildIds;
            using (AppDbContext dbContext = new())
            {
                guildIds = await dbContext.Configs
                    .Where(c => c.EnableAutomaticQotd && 
                    (c.LastSentTimestamp == null ||
                    c.LastSentTimestamp.Value.Day != currentDay) && 
                    ((currentHour == c.QotdTimeHourUtc && currentMinute >= c.QotdTimeMinuteUtc) || currentHour > c.QotdTimeHourUtc))
                    .Select(c => c.GuildId)
                    .ToArrayAsync();
            }

            if (guildIds.Length == 0)
                return;

            Notices.Notice? latestAvailableNotice = Notices.GetLatestAvailableNotice();

            // Send QOTDs in parallel, but limit the degree of parallelism to avoid overwhelming the database or Discord API
            ParallelOptions options = new() 
            {
                MaxDegreeOfParallelism = Program.AppSettings.QotdSendingMaxDegreeOfParallelism
            };
            await Parallel.ForEachAsync(guildIds, options, async (id, ct) =>
            {
                await SendNextQotdIgnoreExceptions(id, latestAvailableNotice);
            });

            await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Sent {guildIds.Length}");
        }

        /// <summary>
        /// Send the next QOTD for the guild and catch and ignore/print all exceptions.
        /// </summary>
        private static async Task SendNextQotdIgnoreExceptions(ulong guildId, Notices.Notice? latestAvailableNotice)
        {
            try
            {
                await QotdSender.FetchGuildAndSendNextQotdAsync(guildId, latestAvailableNotice);
            }
            catch (QotdChannelNotFoundException)
            {
                // This exception is expected if the QOTD channel is not set for the guild.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending QOTD for guild {guildId}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Continuously filters by available QOTDs to send every <see cref="AppSettings.QotdSendingFetchLoopDelayMs"/> milliseconds.
        /// </summary>
        /// <remarks>
        /// Another check will not occur unless all QOTDs of the previous one have been sent.
        /// </remarks>
        public static async Task FetchLoopAsync()
        {
            Console.WriteLine("Started fetch loop.");
            while (true)
            {
                try
                {
                    await Task.Delay(Program.AppSettings.QotdSendingFetchLoopDelayMs);
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
