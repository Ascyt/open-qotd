using CustomQotd.Database;
using CustomQotd.Database.Entities;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
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
                    .Where(c => c.EnableAutomaticQotd && 
                    c.LastSentDay != currentDay && 
                    ((currentHour == c.QotdTimeHourUtc && currentMinute >= c.QotdTimeMinuteUtc) || currentHour > c.QotdTimeHourUtc))
                    .Select(c => c.GuildId)
                    // TODO: order by premium members first
                    .ToArrayAsync();
            }

            for (int i = 0; i < guildIds.Length; i++)
            {
                try
                {
                    DiscordGuild guild = await Program.Client.GetGuildAsync(guildIds[i]);
                }
                catch (NotFoundException)
                {
                    using (var dbContext = new AppDbContext())
                    {
                        Config? config = await dbContext.Configs.Where(c => c.GuildId == guildIds[i]).FirstOrDefaultAsync();

                        if (config is null)
                            continue;

                        dbContext.Configs.Remove(config);

                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"Removed dead guild with ID {guildIds[i]}");
                    }
                    continue;
                }

                try
                {
                    await QotdSender.SendNextQotd(guildIds[i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in SendQotdAsync:\n" + ex.Message);
                    continue;
                }
            }

            if (guildIds.Length > 0)
                await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Sent {guildIds.Length}");
        }

        public static async Task FetchLoopAsync()
        {
            Console.WriteLine("Started fetch loop");
            while (true)
            {
                try
                {
                    await Task.Delay(1_000);
                    //await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Check time");
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
