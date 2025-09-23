using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Exceptions;

namespace OpenQotd.Bot
{
    public static class CommandRequirements
    {
        /// <summary>
        /// Checks if the config has been initialized using `/config initialize` for the current guild.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<Config?> TryGetConfig(CommandContext context)
        {
            (Config?, string?) result = await TryGetConfigAsync(context.Guild!.Id, context.User.Id);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="TryGetConfigAsync(DiscordGuild)"/>.
        /// </summary>
        public static async Task<Config?> TryGetConfig(ComponentInteractionCreatedEventArgs args)
        {
            (Config?, string?) result = await TryGetConfigAsync(args.Guild!.Id, args.User.Id);

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
        /// Checks whether or not the config has been initialized using `/config initialize` for the specified guild.
        /// </summary>
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> TryGetConfigAsync(ulong guildId, ulong userId)
        {
            using AppDbContext dbContext = new();
            try
            {
                Config? c = await ProfileHelpers.GetSelectedConfigAsync(guildId, userId);

                return (c, null);
            }
            catch (ConfigNotInitializedException)
            {
                return (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
            }
        }

        /// <summary>
        /// Check if a user has admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        /// <param name="config">Config if already fetched, if null it will be fetched from the database.</param>
        public static async Task<bool> UserIsAdmin(CommandContext context, Config? config, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsAdmin(context.Guild!, context.Member!, config);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }
        /// <summary>
        /// See <see cref="UserIsAdmin(CommandContext, Config?, bool)"/>.
        /// </summary>
        /// <returns>(If config is initialized, error message if not)</returns>
        public static async Task<(bool, string?)> UserIsAdmin(DiscordGuild guild, DiscordMember member, Config? config)
        {
            if (member.Permissions.HasPermission(DiscordPermission.Administrator))
                return (true, null);

            if (config is null)
            {
                using AppDbContext dbContext = new();

                (config, string? error) = await TryGetConfigAsync(guild.Id, member.Id);

                if (config is null)
                    return (false, error);
            }

            ulong roleId = config.AdminRoleId;

            if (member.Roles.Any(role => role.Id == roleId))
            {
                return (true, null);
            }

            DiscordRole role;
            try
            {
                role = await guild.GetRoleAsync(roleId);
            }
            catch (NotFoundException)
            {
                return (false,
                    $"The role in the admin_role config value with ID {roleId} could not be found.\n\n" +
                        $"*It can be set using `/config set admin_role [role]`.*");
            }

            return (false,
                $"You need to have the \"{role.Mention}\" role or Server Administrator permission to be able to run this command.");
        }

        /// <summary>
        /// Checks if a user has basic or admin permission.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<bool> UserIsBasic(CommandContext context, Config? config, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsBasic(context.Guild!, context.Member!, config);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="UserIsBasic(CommandContext, Config?, bool)"/>.
        /// </summary>
        public static async Task<bool> UserIsBasic(ComponentInteractionCreatedEventArgs args, Config? config)
        {
            (bool, string?) result = await UserIsBasic(args.Guild!, await args.Guild.GetMemberAsync(args.User.Id), config);

            if (!result.Item1)
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
        /// See <see cref="UserIsBasic(CommandContext, Config?, bool)"/>.
        /// </summary>
        public static async Task<(bool, string?)> UserIsBasic(DiscordGuild guild, DiscordMember member, Config? config)
        {
            if (member.Permissions.HasPermission(DiscordPermission.Administrator))
                return (true, null);

            if (config is null)
            {
                using AppDbContext dbContext = new();

                (config, string? error) = await TryGetConfigAsync(guild.Id, member.Id);

                if (config is null)
                    return (false, error);
            }

            ulong? roleId = config.BasicRoleId;

            if (roleId == null)
                return (true, null);

            if (member.Roles.Any(role => role.Id == roleId) && !(await UserIsAdmin(guild, member, config)).Item1)
            {
                return (true, null);
            }

            DiscordRole role;
            try
            {
                role = await guild.GetRoleAsync(roleId.Value);
            }
            catch (NotFoundException)
            {
                return (false,
                    $"The role in the basic_role config value with ID {roleId} could not be found.\n\n" +
                        $"*It can be set using `/config set basic_role [role]`.*");
            }

            return (false,
                $"You need to have the \"{role.Mention}\" role or Server Administrator permission to be able to run this command.");
        }

        /// <summary>
        /// Check if adding <see cref="additionalAmount"/> questions would exceed the maximum allowed amount of questions per guild.
        /// </summary>
        /// <remarks>
        /// The maximum amount is given by <see cref="AppSettings.QuestionsPerGuildMaxAmount"/>.
        /// </remarks>
        public static async Task<bool> IsWithinMaxQuestionsAmount(CommandContext context, int additionalAmount)
        {
            if (additionalAmount < 0)
                return false;

            using AppDbContext dbContext = new();

            int currentAmount = dbContext.Questions
                .Where(q => q.GuildId == context.Guild!.Id)
                .Count();

            bool isWithinLimit = currentAmount + additionalAmount <= Program.AppSettings.QuestionsPerGuildMaxAmount;

            if (!isWithinLimit)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(
                        $"It is not allowed to have more than **{Program.AppSettings.QuestionsPerGuildMaxAmount}** questions in a guild. " +
                        $"There are currently {currentAmount} questions, and adding {additionalAmount} more would exceed the limit, therefore no questions have been added."));
            }

            return isWithinLimit;
        }
    }
}
