using OpenQotd.Bot.Commands;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;

namespace OpenQotd.Bot.EventHandlers
{
    public static class SuggestionsEventHandlers
    {

        /// <returns>(Config, Question, Suggestion notification message?)?</returns>
        public static async Task<(Config, Question, DiscordMessage?)?> GetSuggestionData(DiscordClient client, ComponentInteractionCreatedEventArgs args, int questionGuildDepedentId)
        {
            Config? config;
            using (AppDbContext dbContext = new())
            {
                config = await dbContext.Configs
                    .Where(q => q.GuildId == args.Guild.Id)
                    .FirstOrDefaultAsync();
            }
            if (config is null)
            {
                await RespondWithError(args, $"Config not found or not initialized yet.");
                return null;
            }

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == args.Guild.Id && q.GuildDependentId == questionGuildDepedentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await RespondWithError(args, $"Question with ID `{questionGuildDepedentId}` not found.");
                return null;
            }

            if (config.SuggestionsChannelId is null || question.SuggestionMessageId is null)
                return (config, question, null);

            DiscordChannel? channel = await GeneralHelpers.GetDiscordChannel(config.SuggestionsChannelId.Value, args.Guild);
            if (channel is null)
                return (config, question, null);

            DiscordMessage? message = await GeneralHelpers.GetDiscordMessage(question.SuggestionMessageId.Value, channel);

            return (config, question, message);
        }
        /// <returns>(Config, Question, Suggestion notification message?)?</returns>
        public static async Task<(Config, Question, DiscordMessage?)?> GetSuggestionData(DiscordClient client, ModalSubmittedEventArgs args, int questionGuildDepedentId)
        {
            Config? config;
            using (AppDbContext dbContext = new())
            {
                config = await dbContext.Configs
                    .Where(q => q.GuildId == args.Interaction.Guild!.Id)
                    .FirstOrDefaultAsync();
            }
            if (config is null)
            {
                await RespondWithError(args, $"Config not found or not initialized yet.");
                return null;
            }

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == args.Interaction.Guild!.Id && q.GuildDependentId == questionGuildDepedentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await RespondWithError(args, $"Question with ID `{questionGuildDepedentId}` not found.");
                return null;
            }

            if (config.SuggestionsChannelId is null || question.SuggestionMessageId is null)
                return (config, question, null);

            DiscordChannel? channel = await GeneralHelpers.GetDiscordChannel(config.SuggestionsChannelId.Value, args.Interaction.Guild);
            if (channel is null)
                return (config, question, null);

            DiscordMessage? message = await GeneralHelpers.GetDiscordMessage(question.SuggestionMessageId.Value, channel);

            return (config, question, message);
        }

        public static async Task RespondWithError(ComponentInteractionCreatedEventArgs args, string error)
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.AddEmbed(
                GenericEmbeds.Error(error)
                );

            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(builder));
        }
        public static async Task RespondWithError(ModalSubmittedEventArgs args, string error)
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.AddEmbed(
                GenericEmbeds.Error(error)
                );

            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(builder));
        }

        public static async Task SuggestionsAcceptButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int guildDependentId)
        {
            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, guildDependentId);
            if (suggestionData is null)
                return;

            await SuggestionsCommands.AcceptSuggestionNoContextAsync(suggestionData.Value.Item2, suggestionData.Value.Item3, args, null);
        }
        public static async Task SuggestionsDenyButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int guildDependentId)
        {
            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == args.Guild.Id && q.GuildDependentId == guildDependentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await RespondWithError(args, $"Question with ID `{guildDependentId}` not found.");
                return;
            }

            DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
                .WithTitle(question.Text!.Length > 32 ? $"Denial of \"{question.Text.Substring(0, 32)}…\"" : $"Denial of \"{question.Text}\"")
                .WithCustomId($"suggestions-deny/{guildDependentId}")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Denial Reason", customId: "reason", placeholder: "This will be sent to the user.", max_length: 1024, required: true, style: DiscordTextInputStyle.Paragraph));

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        }

        public static async Task SuggestionsDenyReasonModalSubmitted(DiscordClient client, ModalSubmittedEventArgs args, int guildDependentId)
        {
            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, guildDependentId);
            if (suggestionData is null)
                return;

            string reason = args.Values["reason"];

            await SuggestionsCommands.DenySuggestionNoContextAsync(suggestionData.Value.Item2, suggestionData.Value.Item3, args, null, reason);
        }
    }
}