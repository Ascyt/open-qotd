using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Permissions.Api
{
    public static class SuggestionsMod
    {
        /// <summary>
        /// Checks if a user has suggestions mod or admin permission. This does NOT check whether or not the command was run in the suggestions channel.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<bool> CheckAsync(CommandContext context, Config? config, bool responseOnError = true)
        {
            (bool, string?) result = await CheckAsync(context.Guild!, context.Member!, config);

            if (!result.Item1 && responseOnError)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(result.Item2!));
            }

            return result.Item1;
        }

        /// <summary>
        /// See <see cref="CheckAsync(CommandContext, Config?, bool)"/>.
        /// </summary>
        /// <param name="member">Optionally provided to avoid re-fetching</param>
        public static async Task<bool> CheckAsync(InteractionCreatedEventArgs args, Config? config, DiscordMember? member = null, bool responseOnError=true)
        {
            (bool, string?) result = await CheckAsync(args.Interaction.Guild!, member ?? await args.Interaction.Guild!.GetMemberAsync(args.Interaction.User!.Id), config);

            if (!result.Item1 && responseOnError)
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
        /// See <see cref="CheckAsync(CommandContext, Config?, bool)"/>.
        /// </summary>
        public static async Task<(bool, string?)> CheckAsync(DiscordGuild guild, DiscordMember member, Config? config)
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

            ulong? modRoleId = config.SuggestionsModRoleId;

            if (modRoleId == null || member.Roles.Any(role => role.Id == modRoleId) || 
                (await Api.Admin.CheckAsync(guild, member, config)).Item1)
                return (true, null);
            
            if (UncategorizedCommands.DebugCommand.sudoUserIds.Contains(member.Id))
                return (true, null);

            DiscordRole role;
            try
            {
                role = await guild.GetRoleAsync(modRoleId.Value);
            }
            catch (NotFoundException)
            {
                return (false,
                    $"The role in the suggestions mod_role config value with ID `{modRoleId}` could not be found.\n\n" +
                        $"*It can be set using `/config set suggestions mod_role [role]`.*");
            }

            if ((await Api.Admin.CheckAsync(guild, member, config)).Item1)
                return (true, null);

            return (false,
                $"You need to have the \"{role.Mention}\" role or admin permission to be able to do this.");
        }
    }
}
