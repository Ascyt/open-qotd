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

        /// <returns>(Config, Question), null on error</returns>
        public static async Task<Question?> TryGetSuggestion(InteractionCreatedEventArgs args, Config config, int questionGuildDepedentId)
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

            return question;
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

        public static async Task SuggestionsAcceptButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDepedentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            Question? suggestion = await TryGetSuggestion(args, config, questionGuildDepedentId);
            if (suggestion is null)
                return;

            await SuggestionsAcceptDenyHelpers.AcceptSuggestionAsync(suggestion, config, args, null);
        }
        public static async Task SuggestionsDenyButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDependentId)
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

        public static async Task SuggestionsDenyReasonModalSubmitted(ModalSubmittedEventArgs args, int profileId, int guildDependentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            Question? suggestion = await TryGetSuggestion(args, config, guildDependentId);
            if (suggestion is null)
                return;

            string reason = args.Values["reason"];

            await SuggestionsAcceptDenyHelpers.DenySuggestionAsync(suggestion, config, args, null, reason);
        }
    }
}