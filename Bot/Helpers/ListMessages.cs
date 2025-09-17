using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Text;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using System.Runtime.CompilerServices;

namespace OpenQotd.Bot.Helpers
{
    internal struct PageInfo<T>
    {
        public required T[] Elements;
        public required int CurrentPage;
        public required int TotalPagesCount;
        public required int TotalElementsCount;
        public required int ElementsPerPage;
    }

    /// <summary>
    /// Helper for sending list messages with pagination buttons.
    /// </summary>
    internal static class ListMessages
    {
        /// <summary>
        /// Fetch the database for a list message with pagination.
        /// </summary>
        public delegate Task<PageInfo<T>> FetchDb<T>(int page);

        /// <summary>
        /// Convert an element to a string for display in the list message.
        /// </summary>
        public delegate string ElementToString<T>(T element, int rank);

        /// <summary>
        /// Automatic list message with pagination buttons.
        /// </summary>
        private static string DefaultElementToString<T>(T element, int rank) 
            => $"{rank}. {element}";
        public static async Task SendNew<T>(CommandContext context, int initialPage, string title, FetchDb<T> fetchDb, ElementToString<T>? elementToString=null)
        {
            if (initialPage < 1)
            {
                initialPage = 1;
            }

            elementToString ??= DefaultElementToString;

            PageInfo<T> pi = await fetchDb(initialPage);

            await context.RespondAsync(
                GenerateNew(pi, title, elementToString)
                );

            if (pi.TotalPagesCount == 0)
                return;

            DiscordMessage? message = await context.GetResponseAsync();
            if (message is null)
            {
                IAsyncEnumerable<DiscordMessage> enumerable = context.Channel.GetMessagesAsync(limit: 1);
                await foreach (DiscordMessage item in enumerable)
                {
                    message = item;
                }
            }

            if (message is null)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error("Failed to get the response message.", title: title));
                return;
            }

            InteractivityResult<ComponentInteractionCreatedEventArgs> result;
            try
            {
               result = await message.WaitForButtonAsync();
            }
            catch (ArgumentException)
            {
                // ArgumentException is thrown by WaitForButtonAsync when the message does not contain any interactive buttons.
                // In this case, there is nothing to wait for, so we simply return.
                return;
			}

            while (!result.TimedOut && result.Result?.Id != null)
            {
                bool messageDelete = false;
                switch (result.Result.Id)
                {
                    case "first":
                        pi.CurrentPage = 1;
                        break;
                    case "backward":
                        pi.CurrentPage--;
                        break;
                    case "forward":
                        pi.CurrentPage++;
                        break;
                    case "last":
                        pi.CurrentPage = pi.TotalPagesCount;
                        break;
                    case "redirect":
                        pi.CurrentPage = pi.TotalPagesCount;
                        messageDelete = true;
                        break;
                }

                pi = await fetchDb(pi.CurrentPage);

                if (messageDelete)
                {
                    await message.DeleteAsync();
                    DiscordMessageBuilder newMessageContent = GenerateNew(pi, title, elementToString);
                    message = await context.Channel.SendMessageAsync(newMessageContent);
                }
                else
                {
                    DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();
                    EditExisting(pi, title, builder, elementToString);

                    await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);
                }

                result = await message.WaitForButtonAsync();
            }

            await message.ModifyAsync(GenerateNew(pi, title, elementToString, includeButtons: false));
        }

        /// <summary>
        /// Generate a MessageBuilder for a list of elements. Assumes it is already filtered by page
        /// </summary>
        private static DiscordMessageBuilder GenerateNew<T>(PageInfo<T> pi, string title, ElementToString<T> elementToString, bool includeButtons = true)
        {
            DiscordMessageBuilder message = new();

            if (pi.TotalPagesCount == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"No elements.", title: title));
                return message;
            }

            if (pi.Elements.Length == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"Page {pi.CurrentPage} does not exist.", title: title));
                if (includeButtons)
                {
                    message.AddActionRowComponent(
                            new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", $"Go to last page")
                        );
                }
                return message;
            }

            StringBuilder sb = new();

            for (int i = 0; i < pi.Elements.Length; i++)
            {
                T element = pi.Elements[i];
                sb.AppendLine(elementToString(element, (pi.CurrentPage - 1) * pi.ElementsPerPage + i + 1));
            }

            message.AddEmbed(
                GenericEmbeds.Custom(message: sb.ToString(), title: title)
                .WithFooter($"Page {pi.CurrentPage} of {pi.TotalPagesCount} \x2022 {pi.TotalElementsCount} elements"));

            if (!includeButtons || pi.TotalPagesCount < 2)
                return message;

            message.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", disabled: pi.CurrentPage == pi.TotalPagesCount),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", disabled: pi.CurrentPage == pi.TotalPagesCount)
                );

            return message;
        }
        /// <summary>
        /// Edit a message for a list of elements. Assumes it is already filtered by page
        /// </summary>
        public static void EditExisting<T>(PageInfo<T> pi, string title, DiscordInteractionResponseBuilder message, ElementToString<T> elementToString, bool includeButtons = true)
        {
            if (pi.TotalPagesCount == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"No elements.", title: title));
                return;
            }

            if (pi.Elements.Length == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"Page {pi.CurrentPage} does not exist.", title: title));
                if (includeButtons)
                {
                    message.AddActionRowComponent(
                        new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", "Go to last page")
                    );
                }
                return;
            }

            StringBuilder sb = new();

            for (int i = 0; i < pi.Elements.Length; i++)
            {
                T element = pi.Elements[i];
                sb.AppendLine(elementToString(element, (pi.CurrentPage - 1) * pi.ElementsPerPage + i + 1));
            }

            message.AddEmbed(
                GenericEmbeds.Custom(message:sb.ToString(), title:title)
                .WithFooter($"Page {pi.CurrentPage} of {pi.TotalPagesCount} \x2022 {pi.TotalElementsCount} elements"));

            if (pi.TotalPagesCount < 2)
                return;

            if (includeButtons)
            {
                message.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "<<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", disabled: pi.CurrentPage == pi.TotalPagesCount),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">>", disabled: pi.CurrentPage == pi.TotalPagesCount)
                );
            }

            return;
        }
    }
}
