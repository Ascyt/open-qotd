using CustomQotd.Database;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace CustomQotd.Features.Helpers
{
    public class GeneralHelpers
    {
        /// <summary>
        /// Get a channel from an ID
        /// </summary>
        /// <param name="idObject">The ID as a string as an object</param>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordChannel?> GetDiscordChannel(object? idObject, ulong? guildId = null, CommandContext? commandContext = null)
        {
            if (guildId == null && commandContext == null)
                throw new ArgumentNullException(nameof(guildId));

            if (idObject == null) 
                return null;

            string idString = idObject.ToString() ?? "";
            ulong id;
            DiscordChannel channel;

            try
            {
                if (!ulong.TryParse(idString, out id))
                {
                    return null;
                }

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
