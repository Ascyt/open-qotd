using CustomQotd.Bot.Database;
using CustomQotd.Bot.Database.Entities;
using CustomQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Bot
{
    public static class CommandRequirements
    {
        /// <summary>
        /// Check if the config has been initialized using /config initialize for the current guild. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<Config?> TryGetConfig(CommandContext context)
        {
            (Config?, string?) result = await IsConfigInitialized(context.Guild!);

            if (result.Item1 is null)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }
        public static async Task<Config?> TryGetConfig(ComponentInteractionCreatedEventArgs args)
        {
            (Config?, string?) result = await IsConfigInitialized(args.Guild!);

            if (result.Item1 is null)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
                response.IsEphemeral = true;

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }
        /// <returns>(config if initialized, error if not)</returns>
        public static async Task<(Config?, string?)> IsConfigInitialized(DiscordGuild guild)
        {
            using (var dbContext = new AppDbContext())
            {
                Config? c = await dbContext.Configs.FirstOrDefaultAsync(c => c.GuildId == guild.Id);

                return c is null ? 
                    (null, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.") : 
                    (c, null);
            }
        }

        /// <summary>
        /// Check if a user has admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        /// <param name="config">
        /// Config if already fetched, if `null` it will be fetched from the database.
        /// </param>
        public static async Task<bool> UserIsAdmin(CommandContext context, Config? config, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsAdmin(context.Guild!, context.Member!, config);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }
        /// <returns>(If config is initialized, error message if not)</returns>
        public static async Task<(bool, string?)> UserIsAdmin(DiscordGuild guild, DiscordMember member, Config? config)
        {
            if (member.Permissions.HasPermission(DiscordPermission.Administrator))
                return (true, null);

            if (config is null)
            {
                using (var dbContext = new AppDbContext())
                {
                    config = await dbContext.Configs
                        .Where(c => c.GuildId == guild.Id)
                        .FirstOrDefaultAsync();

                    if (config is null)
                        return (false, "The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
                }
            }

            ulong roleId = config.AdminRoleId;

            if (!member.Roles.Any(role => role.Id == roleId))
            {
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

            return (true, null);
        }

        /// <summary>
        /// Check if a user has basic or admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> UserIsBasic(CommandContext context, Config? config, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsBasic(context.Guild!, context.Member!, config);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }

        public static async Task<bool> UserIsBasic(ComponentInteractionCreatedEventArgs args, Config? config)
        {
            (bool, string?) result = await UserIsBasic(args.Guild!, await args.Guild.GetMemberAsync(args.User.Id), config);

            if (!result.Item1)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
                response.IsEphemeral = true;

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }

        public static async Task<(bool, string?)> UserIsBasic(DiscordGuild guild, DiscordMember member, Config? config)
        {
            if (member.Permissions.HasPermission(DiscordPermission.Administrator))
                return (true, null);

            if (config is null)
            {
                using (var dbContext = new AppDbContext())
                {
                    config = await dbContext.Configs
                        .Where(c => c.GuildId == guild.Id)
                        .FirstOrDefaultAsync();

                    if (config is null)
                        return (false, "The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
                }
            }

            ulong? roleId = config.BasicRoleId;

            if (roleId == null)
                return (true, null);

            if (!member.Roles.Any(role => role.Id == roleId) && !(await UserIsAdmin(guild, member, config)).Item1)
            {
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

            return (true, null);
        }

        public const int MAX_QUESTIONS_AMOUNT = 1024 * 64; // 65k questions per guild
        public static async Task<bool> WithinMaxQuestionsAmount(CommandContext context, int additionalAmount)
        {
            if (additionalAmount < 0)
                return false;

            using (var dbContext = new AppDbContext())
            {
                int currentAmount = dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id)
                    .Count();

                bool isWithinLimit = currentAmount + additionalAmount <= MAX_QUESTIONS_AMOUNT;

                if (!isWithinLimit)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(
                            $"It is not allowed to have more than **{MAX_QUESTIONS_AMOUNT}** questions in a guild. " +
                            $"There are currently {currentAmount} questions, and adding {additionalAmount} more would exceed the limit, therefore no questions have been added."));
                }

                return isWithinLimit;
            }
        }
    }
}
