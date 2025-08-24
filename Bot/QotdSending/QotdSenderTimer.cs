using CustomQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Bot.QotdSending
{
    public class QotdSenderTimer
    {
        public static async Task SendQotdsAsync()
        {
            int currentDay = DateTime.UtcNow.Day;
            int currentHour = DateTime.UtcNow.Hour;
            int currentMinute = DateTime.UtcNow.Minute;

            ulong[] guildIds;
            using (var dbContext = new AppDbContext())
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
                MaxDegreeOfParallelism = 10 
            };
            await Parallel.ForEachAsync(guildIds, options, async (id, ct) =>
            {
                await SendNextQotdIgnoreExceptions(id, latestAvailableNotice);
            });

            await Console.Out.WriteLineAsync($"[{DateTime.UtcNow:O}] Sent {guildIds.Length}");
        }

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

        public static async Task FetchLoopAsync()
        {
            Console.WriteLine("Started fetch loop.");
            while (true)
            {
                try
                {
                    await Task.Delay(100);
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
