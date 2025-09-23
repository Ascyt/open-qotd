using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.Helpers
{

    internal static class ProfileHelpers
    {
        /// <summary>
        /// Returns the config for the profile selected by the user in the guild, or the guild's default profile if none is selected.
        /// </summary>
        /// <exception cref="Exceptions.ConfigNotInitializedException"></exception>
        public static async Task<Config> GetSelectedConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();

            // Try to get the config for the user's selected profile first
            Config? config = await dbContext.GuildUsers
                .Where(guildUser => (guildUser.GuildId == guildId && guildUser.UserId == userId))
                .Join(
                    dbContext.Configs,
                    guildUser => new { guildUser.GuildId, ProfileId = guildUser.SelectedProfileId },
                    config => new { config.GuildId, config.ProfileId },
                    (guildUser, config) => config
                )
                .FirstOrDefaultAsync();

            if (config is not null)
            {
                return config;
            }

            // If the user has no selected profile, return the guild's default profile or any existing profile
            return await GetDefaultConfigAsync(guildId);
        }

        /// <summary>
        /// Returns the profile ID selected by the user in the guild, or the guild's default profile if none is selected.
        /// </summary>
        public static async Task<int?> GetSelectedProfileIdAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();

            int? foundProfileId = await dbContext.GuildUsers
                .Where(guildUser => guildUser.GuildId == guildId && guildUser.UserId == userId)
                .Select(guildUser => (int?)guildUser.SelectedProfileId)
                .FirstOrDefaultAsync();

            return foundProfileId;
        }

        /// <summary>
        /// Returns the default profile ID for the guild, or any existing profile if no default is set.
        /// </summary>
        /// <exception cref="Exceptions.ConfigNotInitializedException"></exception>
        public static async Task<Config> GetDefaultConfigAsync(ulong guildId)         
        {
            using AppDbContext dbContext = new();

            Config? defaultConfig = await dbContext.Configs
                .Where(config => config.GuildId == guildId && config.IsDefaultProfile)
                .FirstOrDefaultAsync();
            if (defaultConfig is not null)
            {
                return defaultConfig;
            }

            Config? existingConfig = await dbContext.Configs
                .Where(config => config.GuildId == guildId)
                .FirstOrDefaultAsync();

            if (existingConfig is not null)
            {
                return existingConfig;
            }

            throw new Exceptions.ConfigNotInitializedException();
        }
    }
}
