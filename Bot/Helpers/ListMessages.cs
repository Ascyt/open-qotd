using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Text;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using DSharpPlus.Commands.Processors.SlashCommands;

namespace OpenQotd.Helpers
{
    internal struct PageInfo<T>
    {
        public required T[] Elements;
        public required int CurrentPage;
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
        /// Default value for <see cref="ElementToString{T}"/>.
        /// </summary>
        private static string DefaultElementToString<T>(T element, int rank) 
            => $"{rank}. {element}";

        /// <summary>
        /// Automatic list message with pagination buttons.
        /// </summary>
        public static async Task SendNew<T>(CommandContext context, int initialPage, string title, FetchDb<T> fetchDb, ElementToString<T>? elementToString=null)
        {
            if (initialPage < 1)
            {
                initialPage = 1;
            }

            elementToString ??= DefaultElementToString;

            PageInfo<T> pi = await fetchDb(initialPage);

            int totalPagesCount = (int)Math.Ceiling(pi.TotalElementsCount / (double)pi.ElementsPerPage);

            DiscordMessageBuilder newMessage = new();
            AddComponents(newMessage, pi, totalPagesCount, title, elementToString);

            await context.RespondAsync(newMessage);

            if (totalPagesCount == 0)
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
                if (result.Result.User.Id != context.User.Id)
                {
                    await EventHandlers.EventHandlers.RespondWithError(result.Result, "You cannot interact with a different user's list message.");
                    result = await message.WaitForButtonAsync();
                    continue;
                }

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
                        pi.CurrentPage = totalPagesCount;
                        break;
                    case "redirect":
                        pi.CurrentPage = totalPagesCount;
                        break;
                }

                pi = await fetchDb(pi.CurrentPage);

                DiscordInteractionResponseBuilder builder = new();
                AddComponents(builder, pi, totalPagesCount, title, elementToString);

                await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, builder);

                result = await message.WaitForButtonAsync();
            }

            DiscordMessageBuilder editedMessage = new();
            AddComponents(editedMessage, pi, totalPagesCount, title, elementToString, includeButtons: false);

            await message.ModifyAsync(editedMessage);
        }

        /// <summary>
        /// Add an embed and buttons (if enabled) with the listing to a MessageBuilder. 
        /// </summary>
        private static void AddComponents<T>(IDiscordMessageBuilder message, PageInfo<T> pi, int totalPagesCount, string title, ElementToString<T> elementToString, bool includeButtons = true)
        {
            if (totalPagesCount == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"No elements.", title: title));
                return;
            }

            if (pi.Elements.Length == 0)
            {
                message.AddEmbed(GenericEmbeds.Error($"Page {pi.CurrentPage} does not exist.", title: title));
                if (includeButtons)
                {
                    // Disabled because of https://github.com/DSharpPlus/DSharpPlus/issues/2376

                    //message.AddActionRowComponent(
                    //        new DiscordButtonComponent(DiscordButtonStyle.Secondary, "redirect", "Go to last page")
                    //    );
                }
                return;
            }

            message.AddEmbed(GetMessageEmbed(pi, totalPagesCount, elementToString, title));

            if (!includeButtons || totalPagesCount < 2)
                return;

            AddPaginationButtonsToMessage(message, pi, totalPagesCount);
        }
        
        /// <summary>
        /// Adds pagination buttons to a message.
        /// </summary>
        private static void AddPaginationButtonsToMessage<T>(IDiscordMessageBuilder messageBuilder, PageInfo<T> pi, int totalPagesCount)
        {
            messageBuilder.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "first", "|<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "backward", "<", disabled: pi.CurrentPage == 1),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "forward", ">", disabled: pi.CurrentPage == totalPagesCount),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "last", ">|", disabled: pi.CurrentPage == totalPagesCount)
            );
        }

        /// <summary>
        /// Generates the embed for the list message.
        /// </summary>
        private static DiscordEmbed GetMessageEmbed<T>(PageInfo<T> pi, int totalPagesCount, ElementToString<T> elementToString, string title)
            => GenericEmbeds.Info(message: ElementListToString(pi, elementToString), title: title)
                .WithFooter($"Page {pi.CurrentPage} of {totalPagesCount} \x2022 {pi.TotalElementsCount} elements");

        /// <summary>
        /// Converts a list of elements to a string for display in the list message.
        /// </summary>
        private static string ElementListToString<T>(PageInfo<T> pi, ElementToString<T> elementToString)
        {
            StringBuilder sb = new();

            for (int i = 0; i < pi.Elements.Length; i++)
            {
                T element = pi.Elements[i];
                sb.AppendLine(elementToString(element, (pi.CurrentPage - 1) * pi.ElementsPerPage + i + 1));
            }

            return sb.ToString();
        }
    }
}
