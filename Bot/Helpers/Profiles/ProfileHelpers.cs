using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Exceptions;

namespace OpenQotd.Bot.Helpers.Profiles
{
    internal static class ProfileHelpers
    {
        public static async Task<Dictionary<int, string>> GetSwitchableProfilesAsync(AbstractContext context, string? filter)
        {
            bool hasAdmin = context.Member!.Permissions.HasPermission(DiscordPermission.Administrator);
            ulong guildId = context.Guild!.Id;

            Config[] configs;
            int? selectedProfileId;
            using (AppDbContext dbContext = new())
            {
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                configs = await dbContext.Configs
                        .Where(c => c.GuildId == guildId && (!hasFilter || EF.Functions.ILike(c.ProfileName, $"%{filter}%")))
                        .OrderBy(c => c.Id)
                        .ToArrayAsync();

                selectedProfileId = await dbContext.GuildUsers
                    .Where(gu => gu.GuildId == guildId && gu.UserId == context.User.Id)
                    .Select(gu => (int?)gu.SelectedProfileId)
                    .FirstOrDefaultAsync();
            }
            selectedProfileId ??= 0;

            Dictionary<int, string> switchableProfiles = [];

            if (hasAdmin)
            {
                switchableProfiles = configs
                    .Where(c => c.ProfileId != selectedProfileId) // Exclude current profile
                    .ToDictionary(c => c.ProfileId, c => c.ProfileName);
            }
            else
            {
                HashSet<ulong> userRoles = [.. context.Member!.Roles.Select(r => r.Id)];
                foreach (Config config in configs)
                {
                    if (config.ProfileId == selectedProfileId)
                        continue; // Exclude current profile

                    bool hasAdminRole = userRoles.Contains(config.AdminRoleId);
                    if (!hasAdminRole)
                        continue;

                    switchableProfiles[config.ProfileId] = config.ProfileName;
                }
            }

            return switchableProfiles;
        }

        /// <summary>
        /// Returns the config for the profile selected by the user in the guild, or the guild's default profile if none is selected.
        /// </summary>
        /// <exception cref="ConfigNotInitializedException"></exception>
        public static async Task<Config> GetSelectedConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();

            // Try to get the config for the user's selected profile first
            Config? config = await dbContext.GuildUsers
                .Where(guildUser => guildUser.GuildId == guildId && guildUser.UserId == userId)
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
            if (profileId is null)
            {
                return "QOTD";
            }
            
            return $"Profile {profileId + 1}";
        }


        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild and, if it has, returns the config.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<Config?> TryGetSelectedConfigAsync(CommandContext context)
        {
            (Config?, string?) result = await TryGetSelectedConfigAsync(context.Guild!.Id, context.User.Id);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="TryGetSelectedConfigAsync(CommandContext)"/>.
        /// </summary>
        public static async Task<Config?> TryGetSelectedConfigAsync(InteractionCreatedEventArgs args)
        {
            (Config?, string?) result = await TryGetSelectedConfigAsync(args.Interaction.Guild!.Id, args.Interaction.User!.Id);

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
        /// See <see cref="TryGetSelectedConfigAsync(CommandContext)"/>.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> TryGetSelectedConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();
            try
            {
                Config? c = await GetSelectedConfigAsync(guildId, userId);

                return (c, null);
            }
            catch (ConfigNotInitializedException)
            {
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
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
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
            }
        }


        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild and, if it has, returns the config.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<Config?> TryGetConfigAsync(CommandContext context, int profileId)
        {
            (Config?, string?) result = await TryGetConfigAsync(context.Guild!.Id, profileId);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }
        /// <summary>
        /// See <see cref="TryGetConfigAsync(CommandContext)"/>.
        /// </summary>
        public static async Task<Config?> TryGetConfigAsync(InteractionCreatedEventArgs args, int profileId)
        {
            (Config?, string?) result = await TryGetConfigAsync(args.Interaction.Guild!.Id, profileId);

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
        /// See <see cref="TryGetConfigAsync(CommandContext)"/>.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> TryGetConfigAsync(ulong guildId, int profileId)
        {
            using AppDbContext dbContext = new();
            try
            {
                Config? c = await dbContext.Configs
                    .Where(c => c.GuildId == guildId && c.ProfileId == profileId)
                    .FirstOrDefaultAsync();

                return (c, null);
            }
            catch (ConfigNotInitializedException)
            {
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
            }
        }
    }
}
