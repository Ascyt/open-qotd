using CustomQotd.Database;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features.QotdSending
{
    public class QotdTimer
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
                    .Where(c => c.LastSentDay != currentDay && ((currentHour == c.QotdTimeHourUtc && currentMinute >= c.QotdTimeMinuteUtc) || currentHour > c.QotdTimeHourUtc))
                    .Select(c => c.GuildId)
                    // TODO: order by premium members first
                    .ToArrayAsync();
            }

            for (int i = 0; i < guildIds.Length; i++)
            {
                await QotdSender.SendNextQotd(guildIds[i]);
            }

            if (guildIds.Length > 0)
                await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Sent {guildIds.Length}");
        }

        public static async Task FetchLoopAsync()
        {
            Console.WriteLine("Started fetch loop");
            while (true)
            {
                await Task.Delay(1_000); 
                await SendQotdsAsync();
            }
        }
    }
}
