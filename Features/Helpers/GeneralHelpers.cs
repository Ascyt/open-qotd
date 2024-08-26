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
        public static async Task<DiscordChannel?> GetDiscordChannel(ulong id, ulong? guildId = null, CommandContext? commandContext = null)
        {
            if (guildId == null && commandContext == null)
                throw new ArgumentNullException(nameof(guildId));

            try
            {
                DiscordGuild? guild = (commandContext != null) ?
                    commandContext.Guild : await Program.Client.GetGuildAsync(id);

                if (guild is null)
                    return null;

                return await guild.GetChannelAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}
