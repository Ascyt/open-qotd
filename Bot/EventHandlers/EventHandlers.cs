using OpenQotd.Bot.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using OpenQotd.Bot.Commands;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace OpenQotd.Bot.EventHandlers
{
    public class EventHandlers
    {
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
                case "first":
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
            Config? config = await CommandRequirements.TryGetConfig(args);

            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            if (!config.EnableSuggestions)
            {
                DiscordEmbed errorEmbed = GenericEmbeds.Error($"Suggestions are not enabled for this server.");

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
            using (AppDbContext dbContext = new())
            {
                int questionsCount = await dbContext.Questions
                    .Where(q => q.GuildId == args.Interaction.GuildId)
                    .CountAsync();

                if (questionsCount >= CommandRequirements.MAX_QUESTIONS_AMOUNT)
                {
                    DiscordEmbed errorEmbed = GenericEmbeds.Error($"The maximum amount of questions for this guild (**{CommandRequirements.MAX_QUESTIONS_AMOUNT}**) has been reached.");
                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .AddEmbed(errorEmbed)
                        .AsEphemeral());
                    return;
                }

                config = await dbContext.Configs
                    .Where(q => q.GuildId == args.Interaction.GuildId)
                    .FirstOrDefaultAsync();
            }
            if (config is null)
            {
                DiscordEmbed errorEmbed = GenericEmbeds.Error("Config not found or not initialized yet.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            if (!config.EnableSuggestions)
            {
                DiscordEmbed errorEmbed = GenericEmbeds.Error($"Suggestions are not enabled for this server.");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            string question = args.Values["text"];

            (bool, DiscordEmbed) result = await SuggestCommand.SuggestNoContextAsync(question, args.Interaction.Guild!, args.Interaction.Channel, args.Interaction.User, config);

            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            messageBuilder.AddEmbed(result.Item2);
            DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder(messageBuilder);
            responseBuilder.IsEphemeral = true;

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);

        }
    }
}
