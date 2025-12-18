using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Core.Configs.Commands;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;

namespace OpenQotd.Core.Permissions.Api
{
    public static class Admin
    {
        /// <summary>
        /// Checks if a user has the "Server Administrator" permission.
        /// </summary>
        public static async Task<bool> UserHasAdministratorPermission(CommandContext context, bool responseOnError = true)
        {
            if (!context.Member!.Permissions.HasPermission(DiscordPermission.Administrator))
            {
                if (UncategorizedCommands.DebugCommand.sudoUserIds.Contains(context.User.Id))
                    return true;

                if (responseOnError)
                {
                    await context.RespondAsync(
                        Helpers.GenericEmbeds.Error("Server Administrator permission is required to run this command.")
                        );
                }
                return false;
            }
            return true;
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
                    Helpers.GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }
        /// <summary>
        /// See <see cref="UserIsAdmin(CommandContext, Config?, bool)"/>.
        /// </summary>
        public static async Task<bool> UserIsAdmin(InteractionCreatedEventArgs args, Config? config, bool responseOnError=true)
        {
            (bool, string?) result = await UserIsAdmin(args.Interaction.Guild!, await args.Interaction.Guild!.GetMemberAsync(args.Interaction.User!.Id), config);

            if (!result.Item1 && responseOnError)
            {
                DiscordInteractionResponseBuilder response = new();
                response.AddEmbed(
                    Helpers.GenericEmbeds.Error(result.Item2!));
                response.IsEphemeral = true;

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
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

                (config, string? error) = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(guild.Id, member.Id);

                if (config is null)
                    return (false, error);
            }

            ulong adminRoleId = config.AdminRoleId;

            if (member.Roles.Any(role => role.Id == adminRoleId))
            {
                return (true, null);
            }

            if (UncategorizedCommands.DebugCommand.sudoUserIds.Contains(member.Id))
                return (true, null);

            DiscordRole role;
            try
            {
                role = await guild.GetRoleAsync(adminRoleId);
            }
            catch (NotFoundException)
            {
                return (false,
                    $"The role in the admin_role config value with ID {adminRoleId} could not be found.\n\n" +
                        $"*It can be set using `/config set general admin_role [role]`.*");
            }

            return (false,
                $"You need to have the \"{role.Mention}\" role or Server Administrator permission to be able to run this command.");
        }
    }
}
