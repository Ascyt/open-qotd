using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Entities;
using OpenQotd.Bot.Helpers;

namespace OpenQotd.Bot.EventHandlers
{
    public class ErrorEventHandlers
    {
        public static async Task CommandErrored(CommandsExtension s, CommandErroredEventArgs e)
        {
            await SendCommandErroredMessage(e.Exception, e.Context);
        }
        public static async Task SendCommandErroredMessage(Exception e, CommandContext context, string? info = null, IEnumerable<DiscordEmbed>? additionalEmbeds = null)
        {
            string message = (info ?? $"An uncaught error occurred from the command you tried to execute.") + "\n" +
                $"If you're unsure what to do here, please feel free to join the [Support Server](<https://open-qotd.ascyt.com/community>) to reach out for help. " +
                $"Make sure to include the below information when you do.\n\n" +
                $"**{e.GetType().Name}**\n" +
                $"> {e.Message}\n\n" +
                $"Stack Trace:\n" +
                $"```\n" +
                $"{e.StackTrace}";

            if (message.Length > 4096 - 5)
                message = string.Concat(message.AsSpan(0, 4096 - 5), "…");

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed(message: message + "\n```", title: "Error (internal)", color: "#800000"));

            if (e is DSharpPlus.Exceptions.UnauthorizedException)
            {
                messageBuilder.AddEmbed(MessageHelpers.GenericWarningEmbed(title: "Hint", message:
                    "This error likely means that the bot is lacking permissions to execute your command.\n" +
                    "The bot needs three different permissions to function correctly:\n" +
                    "- Send Messages\n" +
                    "- Manage Messages\n" +
                    "- Mention @​everyone, @​here and All Roles\n" +
                    "\n" +
                    "If the issue keeps occurring despite these steps, try the following:\n" +
                    "- Verify that the bot is able to send messages and embeds in the relevant channels (qotd channel, suggestion channel, logs channel).\n" +
                    "- Try disabling features such as logging (`/config reset logs_channel`) to help diagnose in which area the problem occurrs.\n" +
                    "- Try kicking the bot from the server and re-inviting it. Your questions should not get deleted by doing this.\n" +
                    "\n" +
                    "Unfortunately, while this is a common issue people are experiencing, it arises from Discord's end - " +
                    "I'm not able to do much more than to add hints to what could be causing the issue.\n" +
                    "\n" +
                    "If you are still experiencing issues with this, don't hesitate to let me know! I'll do my best to be quick to help with any issues."));
            }

            if (additionalEmbeds is not null)
            {
                foreach (DiscordEmbed embed in additionalEmbeds)
                    messageBuilder.AddEmbed(embed);
            }

            await context.RespondAsync(messageBuilder);
        }

    }
}
