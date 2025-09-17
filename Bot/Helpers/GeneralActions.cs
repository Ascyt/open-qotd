using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Database;
using Microsoft.EntityFrameworkCore;

namespace OpenQotd.Bot.Helpers
{
    public class GeneralActions
    {
        /// <summary>
        /// Removes all data associated with a guild from the database.
        /// </summary>
        public static async void RemoveGuildData(ulong guildId)
        {
            using AppDbContext dbContext = new();

            Config? delConfig = await dbContext.Configs.Where(c => c.GuildId == guildId).FirstOrDefaultAsync();

            if (delConfig != null)
            {
                dbContext.Remove(delConfig);
            }

            List<Question> delQuestions = await dbContext.Questions.Where(q => q.GuildId == guildId).ToListAsync();
            dbContext.RemoveRange(delQuestions);

            List<PresetSent> delPresets = await dbContext.PresetSents.Where(ps => ps.GuildId == guildId).ToListAsync();
            dbContext.RemoveRange(delPresets);

            await dbContext.SaveChangesAsync();
        }
    }
}
