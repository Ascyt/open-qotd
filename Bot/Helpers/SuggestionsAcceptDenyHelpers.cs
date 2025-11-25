using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Database;
using OpenQotd.Database.Entities;

namespace OpenQotd.Helpers
{
    public class SuggestionsAcceptDenyHelpers
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

        private static string GetEmbedBody(Question question)
            => $"\"{GeneralHelpers.Italicize(question.Text!)}\"\n" +
                $"By: <@!{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)\n" +
                $"ID: `{question.GuildDependentId}`";

        public static async Task AcceptSuggestionAsync(Question question, Config config, ComponentInteractionCreatedEventArgs? result, CommandContext? context, bool logAndNotify = true)
        {
            if (result is null && context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.User;
            DiscordGuild guild = context?.Guild ?? result!.Guild;

            DiscordMessage? suggestionMessage = await TryGetSuggestionMessage(question, config, guild);

            using (AppDbContext dbContext = new())
            {
                Question? modifyQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (modifyQuestion == null)
                {
                    DiscordEmbed embed = GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found in profile *{config.ProfileName}*.");

                    if (suggestionMessage is null)
                        await context!.Channel.SendMessageAsync(embed);
                    else
                        await suggestionMessage.Channel!.SendMessageAsync(embed
                        );
                    return;
                }

                modifyQuestion.Type = QuestionType.Accepted;
                modifyQuestion.AcceptedByUserId = user.Id;
                modifyQuestion.AcceptedTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                question = modifyQuestion;
            }

            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                string embedBody = GetEmbedBody(question);

                DiscordEmbedBuilder editEmbed = GenericEmbeds.Custom($"{config.QotdShorthandText} Suggestion Accepted", embedBody +
                    $"\n\nAccepted by: {user.Mention}", color: "#20ff20");
                
                if (question.ThumbnailImageUrl is not null)
                {
                    editEmbed.WithThumbnail(question.ThumbnailImageUrl);
                }

                messageBuilder.AddEmbed(editEmbed);

                if (result is null)
                    await suggestionMessage.ModifyAsync(messageBuilder);
                else
                    await result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));

                if (config.EnableSuggestionsPinMessage)
                    await suggestionMessage.UnpinAsync();
            }

            if (!logAndNotify)
                return;

            DiscordUser? suggester;
            try
            {
                suggester = await Program.Client.GetUserAsync(question.SubmittedByUserId);
            }
            catch (NotFoundException)
            {
                suggester = null;
            }
            if (suggester is not null)
            {
                DiscordMessageBuilder userSendMessage = new();
                userSendMessage.AddEmbed(GenericEmbeds.Custom($"{guild.Name}: {config.QotdShorthandText} Suggestion Accepted",
                        $"Your {config.QotdShorthandText} Suggestion:\n" +
                        $"\"**{question.Text}**\"\n\n" +
                        $"Has been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                        $"It is now qualified to appear as **{config.QotdTitleText}** in **{guild.Name}**!",
                        color: "#20ff20"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context is null)
                await Logging.LogUserAction(suggestionMessage!.Channel!, user, config, "Accepted Suggestion", question.ToString());
            else
                await Logging.LogUserAction(context, config, "Accepted Suggestion", question.ToString());
        }

        public static async Task DenySuggestionAsync(Question question, Config config, ModalSubmittedEventArgs? result, CommandContext? context, string? reason, bool logAndNotify = true)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.Interaction.User;
            DiscordGuild guild = context?.Guild ?? result!.Interaction.Guild!;

            DiscordMessage? suggestionMessage = await TryGetSuggestionMessage(question, config, guild);

            using (AppDbContext dbContext = new())
            {
                Question? disableQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (disableQuestion == null)
                {
                    DiscordEmbed embed = GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found in profile *{config.ProfileName}*.");

                    if (suggestionMessage is null)
                        await context!.Channel.SendMessageAsync(embed);
                    else
                        await suggestionMessage.Channel!.SendMessageAsync(embed
                        );
                    return;
                }

                if (config.EnableDeletedToStash)
                {
                    disableQuestion.Type = QuestionType.Stashed;
                }
                else
                {
                    dbContext.Questions.Remove(disableQuestion);
                }

                await dbContext.SaveChangesAsync();
            }

            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                string embedBody = GetEmbedBody(question);

                messageBuilder.AddEmbed(GenericEmbeds.Custom($"{config.QotdShorthandText} Suggestion Denied", embedBody +
                    $"\n\nDenied by: {user.Mention}{(reason != null ? $"\nReason: \"**{reason}**\"" : "")}", color: "#ff2020"));

                await suggestionMessage.ModifyAsync(messageBuilder);
                if (config.EnableSuggestionsPinMessage)
                    await suggestionMessage.UnpinAsync();
            }

            DiscordEmbed responseEmbed = GenericEmbeds.Success($"Successfully Denied {config.QotdShorthandText} Suggestion",
                $"{(reason != null ? $"\nReason: \"**{reason}**\"" : "")}");

            if (result is not null)
            {
                DiscordInteractionResponseBuilder responseBuilder = new();
                responseBuilder.AddEmbed(responseEmbed);
                responseBuilder.AsEphemeral(true);
                await result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);
            }
            if (context is not null)
            {
                await (context as SlashCommandContext)!.RespondAsync(responseEmbed, ephemeral: true);
            }

            if (!logAndNotify)
                return;

            DiscordUser? suggester;
            try
            {
                suggester = await Program.Client.GetUserAsync(question.SubmittedByUserId);
            }
            catch (NotFoundException)
            {
                suggester = null;
            }

            if (suggester is not null)
            {
                DiscordMessageBuilder userSendMessage = new();
                userSendMessage.AddEmbed(GenericEmbeds.Custom($"{guild.Name}: {config.QotdShorthandText} Suggestion Denied",
                        $"Your {config.QotdTitleText} Suggestion:\n" +
                        $"\"**{question.Text}**\"\n\n" +
                        $"Has been :x: **DENIED** :x: {(reason != null ? $"for the following reason:\n" +
                        $"> *{reason}*" : "")}",
                        color: "#ff2020"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context is null)
                await Logging.LogUserAction(suggestionMessage!.Channel!, user, config, "Denied Suggestion" + (config.EnableDeletedToStash ? " (moved to stash)" : ""), $"{question}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
            else
                await Logging.LogUserAction(context, config, "Denied Suggestion" + (config.EnableDeletedToStash ? " (moved to stash)" : ""), $"{question}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
        }
    }
}
