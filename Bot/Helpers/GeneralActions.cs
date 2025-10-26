using OpenQotd.Database.Entities;
using OpenQotd.Database;
using Microsoft.EntityFrameworkCore;

namespace OpenQotd.Helpers
{
    internal static class GeneralActions
    {
        /// <summary>
        /// Removes all data associated with a guild from the database.
        /// </summary>
        public static async void RemoveGuildData(ulong guildId)
        {
            using AppDbContext dbContext = new();

            List<Question> delQuestions = await dbContext.Questions.Where(q => q.GuildId == guildId).ToListAsync();
            dbContext.RemoveRange(delQuestions);

            List<PresetSent> delPresets = await dbContext.PresetSents.Where(ps => ps.GuildId == guildId).ToListAsync();
            dbContext.RemoveRange(delPresets);

            Config[] delConfigs = await dbContext.Configs.Where(c => c.GuildId == guildId).ToArrayAsync();
            dbContext.RemoveRange(delConfigs);

            await dbContext.SaveChangesAsync();
        }
    }
}
