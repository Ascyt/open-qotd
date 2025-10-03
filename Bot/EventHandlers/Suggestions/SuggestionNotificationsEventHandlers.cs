using OpenQotd.Bot.Commands;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Helpers.Profiles;

namespace OpenQotd.Bot.EventHandlers.Suggestions
{
    public static class SuggestionNotificationsEventHandlers
    {

        /// <returns>(Config, Question, Suggestion notification message?)?</returns>
        public static async Task<(Config, Question, DiscordMessage?)?> GetSuggestionData(DiscordClient client, InteractionCreatedEventArgs args, Config config, int questionGuildDepedentId)
        {
            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionGuildDepedentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await RespondWithError(args, $"Question with ID `{questionGuildDepedentId}` for profile \"{config.ProfileName}\" not found.");
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

        public static async Task RespondWithError(InteractionCreatedEventArgs args, string error)
        {
            DiscordMessageBuilder builder = new();
            builder.AddEmbed(
                GenericEmbeds.Error(error)
                );

            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(builder).AsEphemeral());
        }

        public static async Task SuggestionsAcceptButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDepedentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, config, questionGuildDepedentId);
            if (suggestionData is null)
                return;

            await SuggestionsCommands.AcceptSuggestionNoContextAsync(suggestionData.Value.Item2, config, suggestionData.Value.Item3, args, null);
        }
        public static async Task SuggestionsDenyButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDependentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == questionGuildDependentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await RespondWithError(args, $"Question with ID `{questionGuildDependentId}` for profile \"{config.ProfileName}\" not found.");
                return;
            }

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, GeneralHelpers.GetSuggestionDenyModal(config, question));
        }

        public static async Task SuggestionsDenyReasonModalSubmitted(DiscordClient client, ModalSubmittedEventArgs args, int profileId, int guildDependentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            (Config, Question, DiscordMessage?)? suggestionData = await GetSuggestionData(client, args, config, guildDependentId);
            if (suggestionData is null)
                return;

            string reason = args.Values["reason"];

            await SuggestionsCommands.DenySuggestionNoContextAsync(suggestionData.Value.Item2, config, suggestionData.Value.Item3, args, null, reason);
        }
    }
}