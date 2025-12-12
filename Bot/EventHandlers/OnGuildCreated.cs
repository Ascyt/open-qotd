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

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(GenericEmbeds.Info(title:"Thanks for adding OpenQOTD!", message:
                "Thank you for adding OpenQOTD to your server!\n" + 
                "To get started, check out the [documentation](https://open-qotd.ascyt.com/documentation) and use the `/config initialize` and `/config set` commands to set up the bot.\n\n" +
                "If you have any questions or need assistance, please don't hesitate to join the [support server](https://open-qotd.ascyt.com/community), and I hope you have a great experience using OpenQOTD!")
                );
            messageBuilder.AddActionRowComponent(
                new DiscordButtonComponent(
                    customId: "show-general-info-no-prompt/0",
                    style: DiscordButtonStyle.Primary,
                    label: "Help",
                    emoji: new DiscordComponentEmoji("❔")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://open-qotd.ascyt.com/documentation",
                    label: "Docs",
                    emoji: new DiscordComponentEmoji("🧾")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://open-qotd.ascyt.com/community",
                    label: "Support",
                    emoji: new DiscordComponentEmoji("💬")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://ascyt.com/donate",
                    label: "Donate",
                    emoji: new DiscordComponentEmoji("❤️")
                )
            );

            await systemChannel.SendMessageAsync(messageBuilder);
        }
    }
}
