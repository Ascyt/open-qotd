using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Database;
using OpenQotd.Database.Entities;

namespace OpenQotd.Helpers.Suggestions
{
    public class SuggestionsHelpers
    {
        /// <summary>
        /// Gets the sent suggestion message from the suggestions channel if it exists, otherwise returns null.
        /// </summary>
        public static async Task<DiscordMessage?> TryGetSuggestionMessage(Question question, Config config, DiscordGuild guild)
        {
            if (question.SuggestionMessageId is null || config.SuggestionsChannelId is null)
                return null;

            DiscordChannel? suggestionChannel;
            try
            {
                suggestionChannel = await guild.GetChannelAsync(config.SuggestionsChannelId.Value);
            }
            catch (NotFoundException)
            {
                return null;
            }

            if (suggestionChannel is null)
                return null;

            DiscordMessage? suggestionMessage;

            try
            {
                suggestionMessage = await suggestionChannel.GetMessageAsync(question.SuggestionMessageId.Value);
            }
            catch (NotFoundException)
            {
                return null;
            }

            return suggestionMessage;
        }

        public static string GetSuggestionEmbedBody(Question question)
            => $"**Contents:**\n" +
                $"\"{GeneralHelpers.Italicize(question.Text!)}\"\n" +
                $"\n" +
                $"By: <@!{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)\n" +
                $"ID: `{question.GuildDependentId}`";

        /// <summary>
        /// Adds a ping to the message builder if the ping role ID is available.
        /// </summary>
        /// <param name="asHighlight">If enabled, the ping role is only used to highlight the message and not cause an actual ping.</param>
        private static void AddPingIfAvailable(DiscordMessageBuilder messageBuilder, ulong? pingRoleId, bool asHighlight)
        {
            if (pingRoleId is null)
                return;

            if (asHighlight)
            {
                messageBuilder.WithContent($"-# <@&{pingRoleId}>");
            }
            else 
            {
                messageBuilder.WithContent($"<@&{pingRoleId}>");
                messageBuilder.WithAllowedMention(new RoleMention(pingRoleId.Value));
            }
        }

        /// <summary>
        /// Resets the suggestion message belonging to the question to the notification state or tries to create it if it doesn't exist.
        /// </summary>
        /// <remarks>
        /// Should be used after setting a suggestion's content back to "suggested" state or creating a new message with that content.
        /// </remarks>
        public static async Task TryResetSuggestionMessage(Question question, Config config, DiscordGuild guild)
        {
            if (question.SuggestionMessageId is null || config.SuggestionsChannelId is null)
                return;
   
            DiscordChannel? suggestionChannel;
            try
            {
                suggestionChannel = await guild.GetChannelAsync(config.SuggestionsChannelId.Value);
            }
            catch (NotFoundException)
            {
                return;
            }

            if (suggestionChannel is null)
                return;
                
            DiscordMessage? suggestionMessage;
            try
            {
                suggestionMessage = await suggestionChannel.GetMessageAsync(question.SuggestionMessageId.Value);
            }
            catch (NotFoundException)
            {
                suggestionMessage = null;
            }

            DiscordMessageBuilder messageBuilder = await GetSuggestionNotificationMessageBuilder(question, config, guild, pingIsHighlight:true);
            if (suggestionMessage is null)
            {
                suggestionMessage = await suggestionChannel.SendMessageAsync(messageBuilder);
            }
            else 
            {
                await suggestionMessage.ModifyAsync(messageBuilder);
            }
            if (config.EnableSuggestionsPinMessage)
                await suggestionMessage.PinAsync();
        }

        /// <summary>
        /// Sets the suggestion message belonging to the question to the modified state.
        /// </summary>
        /// <remarks>
        /// Should be used when a question's type is changed from "Suggested" to something else without accepting or denying it.
        /// </remarks>
        public static async Task TrySetSuggestionMessageToModified(Question question, Config config, DiscordGuild guild)
        {
            DiscordMessage? suggestionMessage = await TryGetSuggestionMessage(question, config, guild);
            if (suggestionMessage is null)
                return;

            DiscordMessageBuilder messageBuilder = new();

            DiscordEmbedBuilder embed = GenericEmbeds.Custom(
                title: $"{config.QotdShorthandText} Suggestion Modified", 
                message: GetSuggestionEmbedBody(question) + 
                    "\n\n*This suggestion has been modified by staff without being accepted or denied.*",
                color: "#775718ff");
    
            if (question.ThumbnailImageUrl is not null)
            {
                embed.WithThumbnail(question.ThumbnailImageUrl);
            }

            messageBuilder.AddEmbed(embed);
            await suggestionMessage.ModifyAsync(messageBuilder);
            if (config.EnableSuggestionsPinMessage)
                await suggestionMessage.UnpinAsync();  
        }

        public static async Task<DiscordMessageBuilder> GetSuggestionNotificationMessageBuilder(Question question, Config config, DiscordGuild guild, bool pingIsHighlight)
        {
            DiscordMessageBuilder messageBuilder = new();

            if (config.SuggestionsPingRoleId.HasValue)
            {
                AddPingIfAvailable(messageBuilder, config.SuggestionsPingRoleId, pingIsHighlight);
            }

            DiscordEmbedBuilder embed = GenericEmbeds.Custom(
                title: $"A new {config.QotdShorthandText} Suggestion is available!", 
                message: GetSuggestionEmbedBody(question) +
                    (question.ThumbnailImageUrl is not null ? $"\nIncludes a thumbnail image (if it's not visible in this message, it means that the fetching for it failed)." : ""),
                color: "#f0b132");

            if (question.ThumbnailImageUrl is not null)
            {
                embed.WithThumbnail(question.ThumbnailImageUrl);
            }

            messageBuilder.AddEmbed(embed);

            if (question.Notes is not null)
            {
                messageBuilder.AddEmbed(
                    GenericEmbeds.Info(title: "Additional Information", message: GeneralHelpers.Italicize(question.Notes))
                    .WithFooter("Written by the suggester, visible to everyone.")
                    );
            }

            if (question.SuggesterAdminOnlyInfo is not null)
            {
                messageBuilder.AddEmbed(
                    GenericEmbeds.Info(title: "Staff Note", message: GeneralHelpers.Italicize(question.SuggesterAdminOnlyInfo))
                    .WithFooter("Written by the suggester, only visible to staff.")
                    );
            }

            messageBuilder.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, $"suggestions-accept/{config.ProfileId}/{question.GuildDependentId}", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"suggestions-deny/{config.ProfileId}/{question.GuildDependentId}", "Deny")
            );
            return messageBuilder;
        }
    }
}
