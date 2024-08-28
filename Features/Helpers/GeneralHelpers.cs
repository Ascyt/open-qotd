using CustomQotd.Database;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace CustomQotd.Features.Helpers
{
    public class GeneralHelpers
    {
        /// <summary>
        /// Gets a channel from an ID.
        /// </summary>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordChannel?> GetDiscordChannel(ulong id, DiscordGuild? guild = null, ulong? guildId = null, CommandContext? commandContext = null)
        {
            if (guildId == null && commandContext == null && guild is null)
                throw new ArgumentNullException(nameof(guildId));

            try
            {
                if (guild is null)
                {
                    DiscordGuild? actualGuild = (commandContext != null) ?
                        commandContext.Guild : await Program.Client.GetGuildAsync(guildId!.Value);

                    if (actualGuild is null)
                        return null;

                    return await actualGuild.GetChannelAsync(id);
                }
                return await guild.GetChannelAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}
