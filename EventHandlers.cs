using CustomQotd.Features.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;

namespace CustomQotd
{
    public class EventHandlers
    {
        public static async Task CommandErrored(CommandsExtension s, CommandErroredEventArgs e)
        {
            string message = $"**{e.Exception.GetType().Name}**\n> {e.Exception.Message}\n\nStack Trace:\n```\n{e.Exception.StackTrace}";

            if (message.Length > 4096 - 5)
                message = message.Substring(0, 4096 - 5) + "…";

            await e.Context.RespondAsync(MessageHelpers.GenericErrorEmbed(message + "\n```", title: "Error (D#+)"));
        }

        public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            switch (args.Id)
            {
                case "suggest-qotd":
                    await SuggestQotdButtonClicked(client, args);
                    return;
            }

            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.AddEmbed(
                MessageHelpers.GenericErrorEmbed($"Unknown event: `{args.Id}`")
                );

            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(builder));
        }

        private static async Task SuggestQotdButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {

        }
    }
}
