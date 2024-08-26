using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace CustomQotd.Features
{
    public static class CommandRequirements
    {
        /// <summary>
        /// Check if the config has been initialized using /initialize for the current guild. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> IsConfigInitialized(CommandContext context)
        {
            if (!await DatabaseApi.IsConfigInitializedAsync(context.Guild.Id))
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed($"The QOTD bot configuration has not been initialized yet. Use `/initialize` to initialize."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a user has admin permission. This function also handles sending error messages, so it's recommended to end the function if it retuns false.
        /// </summary>
        public static async Task<bool> UserIsAdmin(CommandContext context, bool responseOnError = true)
        {
            if (context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
                return true;

            string roleIdString = (await DatabaseApi.GetConfigValueAsync(context.Guild.Id, DatabaseValues.ConfigType.AdminRoleId)).ToString();

            ulong roleId;
            if (!ulong.TryParse(roleIdString, out roleId))
            {
                if (responseOnError)
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed($"Unable to check for administrator permission because admin_role config value (\"{roleIdString}\") cannot be parsed to ID.\n\n" +
                        $"*You can set it using `/config set admin_role [role]`.*")
                        );

                return false;
            }

            if (!context.Member.Roles.Any(role => role.Id == roleId))
            {
                DiscordRole? role = await context.Guild.GetRoleAsync(roleId);
                if (role is null)
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
                        MessageHelpers.GenericErrorEmbed($"You need to have the \"{role.Name}\" role or Server Administrator permission to be able to run this command.")
                        );
                return false;
            }

            return true;
        }
    }
}
