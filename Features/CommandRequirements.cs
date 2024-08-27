using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features
{
    public static class CommandRequirements
    {
        /// <summary>
        /// Check if the config has been initialized using /initialize for the current guild. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> IsConfigInitialized(CommandContext context)
        {
            using (var dbContext = new AppDbContext())
            {
                if (await dbContext.Configs.AnyAsync(c => c.GuildId == context.Guild.Id))
                {
                    return true;
                }
            }

            await context.RespondAsync(
                MessageHelpers.GenericErrorEmbed($"The QOTD bot configuration has not been initialized yet. Use `/initialize` to initialize."));
            return false;
        }

        /// <summary>
        /// Check if a user has admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> UserIsAdmin(CommandContext context, bool responseOnError = true)
        {
            if (context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                return true;

            ulong roleId;
            using (var dbContext = new AppDbContext())
            {
                roleId = await dbContext.Configs.Where(c => c.GuildId == context.Guild.Id).Select(c => c.AdminRoleId).FirstOrDefaultAsync();
            }

            if (!context.Member.Roles.Any(role => role.Id == roleId))
            {
                DiscordRole role;
                try
                {
                    role = await context.Guild.GetRoleAsync(roleId);
                }
                catch (NotFoundException)
                {
                    if (responseOnError)
                        await context.RespondAsync(
                            MessageHelpers.GenericErrorEmbed($"The role in the admin_role config value with ID {roleId} could not be found.\n\n" +
                            $"*You can set it using `/config set admin_role [role]`.*")
                            );
                    return false;
                }

                if (responseOnError)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed($"You need to have the \"{role.Mention}\" role or Server Administrator permission to be able to run this command.")
                        );
                return false;
            }

            return true;
        }
    }
}
