using CustomQotd.Features.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using CustomQotd.Features.Commands;
using CustomQotd.Database;
using CustomQotd.Database.Entities;
using Microsoft.EntityFrameworkCore;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;

namespace CustomQotd.Features.EventHandlers
{
    public class EventHandlers
    {
        public static async Task CommandErrored(CommandsExtension s, CommandErroredEventArgs e)
        {
            await SendCommandErroredMessage(e.Exception, e.Context);
        }
        public static async Task SendCommandErroredMessage(Exception e, CommandContext context, string? info=null)
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
                message = message.Substring(0, 4096 - 5) + "…";

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed(message: message + "\n```", title: "Error (internal)", color: "#800000"));

            if (e is DSharpPlus.Exceptions.UnauthorizedException || true)
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

            await context.RespondAsync(messageBuilder);
        }

        public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            string[] idArgs = args.Id.Split("/");

            switch (idArgs[0])
            {
                case "suggestions-accept":
                    await SuggestionsEventHandlers.SuggestionsAcceptButtonClicked(client, args, int.Parse(idArgs[1]));
                    return;
                case "suggestions-deny":
                    await SuggestionsEventHandlers.SuggestionsDenyButtonClicked(client, args, int.Parse(idArgs[1]));
                    return;
                case "suggest-qotd":
                    await SuggestQotdButtonClicked(client, args);
                    return;

                case "forward":
                case "backward":
                case "last":
                case "redirect":
                case "reroll":
                    return;
            }

            await SuggestionsEventHandlers.RespondWithError(args, $"Unknown event: `{args.Id}`");
        }

        public static async Task ModalSubmittedEvent(DiscordClient client, ModalSubmittedEventArgs args)
        {
            string[] idArgs = args.Interaction.Data.CustomId.Split("/");

            switch (idArgs[0])
            {
                case "suggestions-deny":
                    await SuggestionsEventHandlers.SuggestionsDenyReasonModalSubmitted(client, args, int.Parse(idArgs[1]));
                    return;
                case "suggest-qotd":
                    await SuggestQotdModalSubmitted(client, args);
                    return;
            }

            await SuggestionsEventHandlers.RespondWithError(args, $"Unknown event: `{args.Interaction.Data.CustomId}`");
        }


        private static async Task SuggestQotdButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            if (!await CommandRequirements.IsConfigInitialized(args) || !await CommandRequirements.UserIsBasic(args))
                return;
                
            Config? config;
            using (var dbContext = new AppDbContext())
            {
                config = await dbContext.Configs
                    .Where(q => q.GuildId == args.Interaction.GuildId)
                    .FirstOrDefaultAsync();
            }
            if (config is null)
            {
                DiscordEmbed errorEmbed = MessageHelpers.GenericErrorEmbed("Config not found or not initialized yet.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            if (!config.EnableSuggestions)
            {
                DiscordEmbed errorEmbed = MessageHelpers.GenericErrorEmbed($"Suggestions are not enabled for this server.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Suggest a Question Of The Day!")
                .WithCustomId("suggest-qotd")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Question", customId: "text", placeholder: $"This will require approval by the staff of this server.", max_length: 256, required: true, style: DiscordTextInputStyle.Short));

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        }

        private static async Task SuggestQotdModalSubmitted(DiscordClient client, ModalSubmittedEventArgs args)
        {
            Config? config;
            using (var dbContext = new AppDbContext())
            {
                config = await dbContext.Configs
                    .Where(q => q.GuildId == args.Interaction.GuildId)
                    .FirstOrDefaultAsync();
            }
            if (config is null)
            {
                DiscordEmbed errorEmbed = MessageHelpers.GenericErrorEmbed("Config not found or not initialized yet.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            if (!config.EnableSuggestions)
            {
                DiscordEmbed errorEmbed = MessageHelpers.GenericErrorEmbed($"Suggestions are not enabled for this server.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            string question = args.Values["text"];

            (bool, DiscordEmbed) result = await SuggestCommand.SuggestNoContextAsync(question, args.Interaction.Guild!, args.Interaction.Channel, args.Interaction.User);

            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            messageBuilder.AddEmbed(result.Item2);
            DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder(messageBuilder);
            responseBuilder.IsEphemeral = true;

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);

        }
    }
}
