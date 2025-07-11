﻿using CustomQotd.Database;
using CustomQotd.Database.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
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
                .WithDescription(message);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <returns>Elements, total elements, total pages, elements per page</returns>
        public delegate Task<(T[], int, int, int)> ListMessageCompleteFetchDb<T>(int page);
        public static async Task ListMessageComplete<T>(CommandContext context, int initialPage, string title, ListMessageCompleteFetchDb<T> fetchDb)
        {
            int page = initialPage;

            if (page < 1)
            {
                page = 1;
            }

            (T[] elements, int totalElements, int totalPages, int elementsPerPage) = await fetchDb(page);

            await context.RespondAsync(
                MessageHelpers.GetListMessage(elements, title, page, totalPages, totalElements, elementsPerPage)
                );

            if (totalPages == 0)
                return;

            DiscordMessage? message = await context.GetResponseAsync();
            if (message == null)
            {
                var enumerable = context.Channel.GetMessagesAsync(limit: 1);
                await foreach (var item in enumerable)
                {
                    message = item;
                }
            }

            var result = await message.WaitForButtonAsync();

            while (!result.TimedOut && result.Result?.Id != null)
            {
                bool messageDelete = false;
                switch (result.Result.Id)
                {
                    case "first":
                        page = 1;
                        break;
                    case "backward":
                        page--;
                        break;
                    case "forward":
                        page++;
                        break;
                    case "last":
                        page = totalPages;
                        break;
                    case "redirect":
                        page = totalPages;
                        messageDelete = true;
                        break;
                }

                (elements, totalElements, totalPages, elementsPerPage) = await fetchDb(page);

                if (messageDelete)
                {
                    await message.DeleteAsync();
                    var newMessageContent = MessageHelpers.GetListMessage(elements, title, page, totalPages, totalElements, elementsPerPage);
                    message = await context.Channel.SendMessageAsync(newMessageContent);
                }
                else
                {
                    DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();
                    MessageHelpers.EditListMessage(elements, title, page, totalPages, totalElements, elementsPerPage, builder);

                    await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
                }

                result = await message.WaitForButtonAsync();
            }

            await message.ModifyAsync(MessageHelpers.GetListMessage(elements, title, page, totalPages, totalElements, elementsPerPage, includeButtons: false));
        }

        /// <summary>
        /// Get a message for a list of elements. Assumes it is already filtered by page
        /// </summary>
        public static DiscordMessageBuilder GetListMessage<T>(T[] elements, string title, int page, int totalPages, int totalElements, int elementsPerPage, bool includeButtons = true)
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
                if (includeButtons)
                {
                    message.AddActionRowComponent(
                            new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", $"Go to page {totalPages}")
                        );
                }
                return message;
            }

            StringBuilder sb = new();

            for (int i = 0; i < elements.Length; i++)
            {
                T element = elements[i];
                sb.AppendLine(element!.ToString()!.Replace("%index%", ((page - 1) * elementsPerPage + i + 1).ToString()));
            }

            message.AddEmbed(
                GenericEmbed(message: sb.ToString(), title: title)
                .WithFooter($"Page {page} of {totalPages} \x2022 {totalElements} elements")); // TODO: Add elements in total

            if (totalPages < 2)
                return message;

            if (includeButtons)
            {
                message.AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", page == 1),
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", page == 1),
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", page == totalPages),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", page == totalPages)
                    );
            }

            return message;
        }
        /// <summary>
        /// Edit a message for a list of elements. Assumes it is already filtered by page
        /// </summary>
        public static void EditListMessage<T>(T[] elements, string title, int page, int totalPages, int totalElements, int elementsPerPage, DiscordInteractionResponseBuilder message, bool includeButtons = true)
        {
            if (totalPages == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"No elements.", title: title));
                return;
            }

            if (elements.Length == 0)
            {
                message.AddEmbed(GenericErrorEmbed($"Page {page} does not exist.", title: title));
                if (includeButtons)
                {
                    message.AddActionRowComponent(
                        new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", $"Go to page {totalPages}")
                    );
                }
                return;
            }

            StringBuilder sb = new();

            for (int i = 0; i < elements.Length; i++)
            {
                T element = elements[i];
                sb.AppendLine(element!.ToString()!.Replace("%index%", ((page - 1) * elementsPerPage + i + 1).ToString()));
            }

            message.AddEmbed(
                GenericEmbed(message:sb.ToString(), title:title)
                .WithFooter($"Page {page} of {totalPages} \x2022 {totalElements} elements"));

            if (totalPages < 2)
                return;

            if (includeButtons)
            {
                message.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", page == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", page == totalPages),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", page == totalPages)
                );
            }

            return;
        }
    }
}
