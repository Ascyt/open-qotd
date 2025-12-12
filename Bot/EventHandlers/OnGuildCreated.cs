using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OpenQotd.EventHandlers.Suggestions;
using OpenQotd.Helpers;

namespace OpenQotd.EventHandlers
{
    public class OnGuildCreated
    {
        public static async Task SendMessage(GuildCreatedEventArgs args)
        {
            DiscordChannel? systemChannel = await args.Guild.GetSystemChannelAsync();
            if (systemChannel is null)
                return;

            await systemChannel.SendMessageAsync(
                GenericEmbeds.Info("todo")
                );
        }
    }
}
