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
            string[] idArgs = args.Id.Split('/');

            switch (idArgs[0])
            {
                case "suggestions-accept":
                    await SuggestionsAcceptButtonClicked(client, args, int.Parse(idArgs[1]));
                    return;
                case "suggestions-deny":
                    await SuggestionsDenyButtonClicked(client, args, int.Parse(idArgs[1]));
                    return;
                case "suggest-qotd":
                    await SuggestQotdButtonClicked(client, args);
                    return;
            }

            await RespondWithError(args, $"Unknown event: `{args.Id}`");
        }

        private static async Task RespondWithError(ComponentInteractionCreatedEventArgs args, string error)
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.AddEmbed(
                MessageHelpers.GenericErrorEmbed(error)
                );

            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(builder));
        }

        /// <returns>(Config, Question, Suggestion notification message?)?</returns>
        private static async Task<(Config, Question, DiscordMessage?)?> GetSuggestionData(DiscordClient client, ComponentInteractionCreatedEventArgs args, int questionGuildDepedentId)
        {
            Config? config;
            using (var dbContext = new AppDbContext())
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
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions
                    .Where(q => q.GuildId == args.Guild.Id && q.GuildDependentId == questionGuildDepedentId)
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

        private static async Task SuggestionsAcceptButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int guildDependentId)
        {
            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, guildDependentId);
            if (suggestionData is null)
                return;

            await SuggestionsCommands.AcceptSuggestionNoContextAsync(suggestionData.Value.Item2, suggestionData.Value.Item3, args, null);
        }
        private static async Task SuggestionsDenyButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int guildDependentId)
        {
            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, guildDependentId);
            if (suggestionData is null)
                return;

            //await SuggestionsCommands.DenySuggestionNoContextAsync(suggestionData.Value.Item2, suggestionData.Value.Item3, args, null);
        }

        private static async Task SuggestQotdButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {

        }
    }
}
