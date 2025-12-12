using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Exceptions;

namespace OpenQotd.Helpers.Profiles
{
    internal static class ProfileHelpers
    {
        /// <summary>
        /// Returns the config for the profile selected by the user in the guild, or the guild's default profile if none is selected.
        /// </summary>
        /// <exception cref="ConfigNotInitializedException"></exception>
        public static async Task<Config> GetSelectedOrDefaultConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();

            // Try to get the config for the user's selected profile first
            Config? config = await dbContext.ProfileSelections
                .Where(selection => selection.GuildId == guildId && selection.UserId == userId)
                .Join(
                    dbContext.Configs,
                    selection => new { selection.GuildId, ProfileId = selection.SelectedProfileId },
                    config => new { config.GuildId, config.ProfileId },
                    (selection, config) => config
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
        public static async Task<int> GetSelectedOrDefaultProfileIdAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();

            int? foundProfileId = await dbContext.ProfileSelections
                .Where(selection => selection.GuildId == guildId && selection.UserId == userId)
                .Select(selection => (int?)selection.SelectedProfileId)
                .FirstOrDefaultAsync();

            if (foundProfileId is not null)
            {
                return foundProfileId.Value;
            }

            return dbContext.Configs
                .Where(config => config.GuildId == guildId && config.IsDefaultProfile)
                .Select(config => config.ProfileId)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns the default config (profile) for the guild, or any existing profile if no default is set.
        /// </summary>
        /// <exception cref="ConfigNotInitializedException"></exception>
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

            throw new ConfigNotInitializedException();
        }

        public static string GenerateProfileName(int? profileId)
        {
            if (profileId == 0)
            {
                return Program.AppSettings.ConfigProfileNameDefault;
            }
            
            return $"Profile {profileId + 1}";
        }


        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild, and, if it has, tries to return the selected config, or otherwise the default config.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<Config?> TryGetSelectedOrDefaultConfigAsync(CommandContext context)
        {
            (Config?, string?) result = await TryGetSelectedOrDefaultConfigAsync(context.Guild!.Id, context.User.Id);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="TryGetSelectedOrDefaultConfigAsync(CommandContext)"/>.
        /// </summary>
        public static async Task<Config?> TryGetSelectedOrDefaultConfigAsync(InteractionCreatedEventArgs args)
        {
            (Config?, string?) result = await TryGetSelectedOrDefaultConfigAsync(args.Interaction.Guild!.Id, args.Interaction.User!.Id);

            if (result.Item1 is null)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    GenericEmbeds.Error(result.Item2!));
                response.IsEphemeral = true;

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="TryGetSelectedOrDefaultConfigAsync(CommandContext)"/>.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> TryGetSelectedOrDefaultConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();
            try
            {
                Config? c = await GetSelectedOrDefaultConfigAsync(guildId, userId);

                return (c, null);
            }
            catch (ConfigNotInitializedException)
            {
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize it, or `/help` for help.");
            }
        }

        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild and, if it has, returns the config.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<Config?> TryGetDefaultConfigAsync(CommandContext context)
        {
            (Config?, string?) result = await TryGetDefaultConfigAsync(context.Guild!.Id);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }
        /// <summary>
        /// See <see cref="TryGetDefaultConfigAsync(CommandContext)"/>.
        /// </summary>
        public static async Task<Config?> TryGetDefaultConfigAsync(InteractionCreatedEventArgs args)
        {
            (Config?, string?) result = await TryGetDefaultConfigAsync(args.Interaction.Guild!.Id);

            if (result.Item1 is null)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    GenericEmbeds.Error(result.Item2!));
                response.IsEphemeral = true;

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }
        /// <summary>
        /// See <see cref="TryGetDefaultConfigAsync(CommandContext)"/>.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> TryGetDefaultConfigAsync(ulong guildId)
        {
            using AppDbContext dbContext = new();
            try
            {
                Config? c = await GetDefaultConfigAsync(guildId);

                return (c, null);
            }
            catch (ConfigNotInitializedException)
            {
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize it, or `/help` for help.");
            }
        }


        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild and, if it has, returns the config.
        /// </summary>
        /// <remarks>
        /// This does not send any error messages.
        /// </remarks>
        public static async Task<Config?> TryGetConfigAsync(CommandContext context, int profileId)
        {
            return await TryGetConfigAsync(context.Guild!.Id, profileId);
        }
        /// <summary>
        /// See <see cref="TryGetConfigAsync(CommandContext)"/>.
        /// </summary>
        public static async Task<Config?> TryGetConfigAsync(InteractionCreatedEventArgs args, int profileId)
        {
            return await TryGetConfigAsync(args.Interaction.Guild!.Id, profileId);
        }
        /// <summary>
        /// See <see cref="TryGetConfigAsync(CommandContext)"/>.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<Config?> TryGetConfigAsync(ulong guildId, int profileId)
        {
            if (profileId == 0)
                return (await TryGetDefaultConfigAsync(guildId)).Item1;

            using AppDbContext dbContext = new();

            return await dbContext.Configs
                .Where(c => c.GuildId == guildId && c.ProfileId == profileId)
                .FirstOrDefaultAsync();
        }
    }
}
