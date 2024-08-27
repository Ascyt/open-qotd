using CustomQotd.Database.Entities;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace CustomQotd.Features.Helpers
{
    /// <summary>
    /// Presets for embed response messages
    /// </summary>
    public static class MessageHelpers
    {
        public static DiscordEmbedBuilder GenericSuccessEmbed(string title, string message) =>
            GenericEmbed(title, message, "#20c020");

        public static DiscordEmbedBuilder GenericErrorEmbed(string message, string title = "Error") =>
            GenericEmbed(title, message, "#ff0000");
        public static DiscordEmbedBuilder GenericWarningEmbed(string message, string title = "Warning") =>
            GenericEmbed(title, message, "#ffc000");

        public static DiscordEmbedBuilder GenericEmbed(string title, string message, string color = "#5865f2") => new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithColor(new DiscordColor(color))
                .WithDescription(message)
                .WithTimestamp(DateTime.UtcNow);

        /// <summary>
        /// Get a message for a list of elements. Assumes it is already filtered by page
        /// </summary>
        public static DiscordMessageBuilder GetListMessage<T>(T[] elements, string title, int page, int totalPages)
        {
            DiscordMessageBuilder message = new();

            if (totalPages == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"No elements.", title: title));
                return message;
            }

            if (elements.Length == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"Page {page} does not exist.", title: title));
                message.AddComponents(
                        new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", $"Go to page {totalPages}")
                    );
                return message;
            }

            StringBuilder sb = new();

            for (int i = 0; i < elements.Length; i++)
            {
                T element = elements[i];
                sb.AppendLine(element!.ToString());
            }

            message.AddEmbed(
                GenericEmbed(message: sb.ToString(), title: title)
                .WithFooter($"Page {page} of {totalPages}"));

            if (totalPages < 2)
                return message;

            message.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", page == totalPages),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", page == totalPages)
                );

            return message;
        }
        /// <summary>
        /// Edit a message for a list of elements. Assumes it is already filtered by page
        /// </summary>
        public static void EditListMessage<T>(T[] elements, string title, int page, int totalPages, DiscordInteractionResponseBuilder message)
        {
            if (totalPages == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"No elements.", title: title));
                return;
            }

            if (elements.Length == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"Page {page} does not exist.", title: title));
                message.AddComponents(
                        new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", $"Go to page {totalPages}")
                    );
                return;
            }

            StringBuilder sb = new();

            for (int i = 0; i < elements.Length; i++)
            {
                T element = elements[i];
                sb.AppendLine(element!.ToString());
            }

            message.AddEmbed(
                GenericEmbed(message:sb.ToString(), title:title)
                .WithFooter($"Page {page} of {totalPages}"));

            if (totalPages < 2)
                return;

            message.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", page == totalPages),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", page == totalPages)
                );

            return;
        }
    }
}
