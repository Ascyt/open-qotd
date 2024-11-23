using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features
{
    public static class CommandRequirements
    {
        /// <summary>
        /// Check if the config has been initialized using /config initialize for the current guild. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> IsConfigInitialized(CommandContext context)
        {
            (bool, string?) result = await IsConfigInitialized(context.Guild!);

            if (!result.Item1)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }
        public static async Task<bool> IsConfigInitialized(ComponentInteractionCreatedEventArgs args)
        {
            (bool, string?) result = await IsConfigInitialized(args.Guild!);

            if (!result.Item1)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }
        /// <returns>(If config is initialized, error message if not)</returns>
        public static async Task<(bool, string?)> IsConfigInitialized(DiscordGuild guild)
        {
            using (var dbContext = new AppDbContext())
            {
                return (await dbContext.Configs.AnyAsync(c => c.GuildId == guild.Id)) ? 
                    (true, null) : 
                    (false, $"The QOTD bot configuration has not been initialized yet. Use `/config initialize` to initialize.");
            }
        }

        /// <summary>
        /// Check if a user has admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> UserIsAdmin(CommandContext context, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsAdmin(context.Guild!, context.Member!);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }
        /// <returns>(If config is initialized, error message if not)</returns>
        public static async Task<(bool, string?)> UserIsAdmin(DiscordGuild guild, DiscordMember member)
        {
            if (member.Permissions.HasPermission(DiscordPermissions.Administrator))
                return (true, null);

            ulong roleId;
            using (var dbContext = new AppDbContext())
            {
                roleId = await dbContext.Configs.Where(c => c.GuildId == guild.Id).Select(c => c.AdminRoleId).FirstOrDefaultAsync();
            }

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
        public static async Task<bool> UserIsBasic(CommandContext context, bool responseOnError = true)
        {
            (bool, string?) result = await UserIsBasic(context.Guild!, context.Member!);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));
            }

            return result.Item1;
        }

        public static async Task<bool> UserIsBasic(ComponentInteractionCreatedEventArgs args)
        {
            (bool, string?) result = await UserIsBasic(args.Guild!, await args.Guild.GetMemberAsync(args.User.Id));

            if (!result.Item1)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    MessageHelpers.GenericErrorEmbed(result.Item2!));

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
            }

            return result.Item1;
        }

        public static async Task<(bool, string?)> UserIsBasic(DiscordGuild guild, DiscordMember member)
        {
            if (member.Permissions.HasPermission(DiscordPermissions.Administrator))
                return (true, null);

            ulong? roleId;
            using (var dbContext = new AppDbContext())
            {
                roleId = await dbContext.Configs.Where(c => c.GuildId == guild.Id).Select(c => c.BasicRoleId).FirstOrDefaultAsync();
            }

            if (roleId == null)
                return (true, null);

            if (!member.Roles.Any(role => role.Id == roleId) && !(await UserIsAdmin(guild, member)).Item1)
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
    }
}
