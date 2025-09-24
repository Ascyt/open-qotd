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
using OpenQotd.Bot.Helpers.Profiles;

namespace OpenQotd.Bot.EventHandlers
{
    public class EventHandlers
    {
        public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            string[] idArgs = args.Id.Split("/");

            if (idArgs.Length == 0)
            {
                await SuggestionsEventHandlers.RespondWithError(args, "Interaction ID is empty");
                return;
            }

            switch (idArgs[0])
            {
                case "suggestions-accept":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionsEventHandlers.SuggestionsAcceptButtonClicked(client, args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggestions-deny":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionsEventHandlers.SuggestionsDenyButtonClicked(client, args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await SuggestQotdButtonClicked(client, args, int.Parse(idArgs[1]));
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

            if (idArgs.Length == 0)
            {
                await SuggestionsEventHandlers.RespondWithError(args, "Interaction ID is empty");
                return;
            }

            switch (idArgs[0])
            {
                case "suggestions-deny":
                    if (!await HasExactlyNArguments(args, idArgs, 2))
                        return;

                    await SuggestionsEventHandlers.SuggestionsDenyReasonModalSubmitted(client, args, int.Parse(idArgs[1]), int.Parse(idArgs[2]));
                    return;
                case "suggest-qotd":
                    if (!await HasExactlyNArguments(args, idArgs, 1))
                        return;

                    await SuggestQotdModalSubmitted(client, args, int.Parse(idArgs[1]));
                    return;
            }

            await SuggestionsEventHandlers.RespondWithError(args, $"Unknown event: `{args.Interaction.Data.CustomId}`");
        }

        private static async Task<bool> HasExactlyNArguments(InteractionCreatedEventArgs args, string[] idArgs, int n)
        {
            if (idArgs.Length - 1 == n)
                return true;

            await SuggestionsEventHandlers.RespondWithError(args, $"Component ID for `{idArgs[0]}` must have exactly {n} arguments (provided is {idArgs.Length}).");
            return false;
        }


        private static async Task SuggestQotdButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int profileId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
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
                .WithTitle("Suggest a new QOTD!")
                .WithCustomId($"suggest-qotd/{config.ProfileId}")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Content", customId: "text", placeholder: $"This will require approval by the staff of this server.", max_length: 256, required: true, style: DiscordTextInputStyle.Short));

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        }

        private static async Task SuggestQotdModalSubmitted(DiscordClient client, ModalSubmittedEventArgs args, int profileId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            using (AppDbContext dbContext = new())
            {
                int questionsCount = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id)
                    .CountAsync();

                if (questionsCount >= Program.AppSettings.QuestionsPerGuildMaxAmount)
                {
                    DiscordEmbed errorEmbed = GenericEmbeds.Error($"The maximum amount of questions for this guild (**{Program.AppSettings.QuestionsPerGuildMaxAmount}**) has been reached.");
                    await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .AddEmbed(errorEmbed)
                        .AsEphemeral());
                    return;
                }
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

            (bool, DiscordEmbed) result = await SuggestCommand.SuggestNoContextAsync(question, config, args.Interaction.Guild!, args.Interaction.Channel, args.Interaction.User);

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(result.Item2);
            DiscordInteractionResponseBuilder responseBuilder = new(messageBuilder)
            {
                IsEphemeral = true
            };

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);

        }
    }
}
