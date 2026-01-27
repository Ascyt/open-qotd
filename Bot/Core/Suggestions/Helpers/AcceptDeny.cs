using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Suggestions.Helpers
{
    public class AcceptDeny
    {
        public static async Task AcceptSuggestionAsync(Question question, Config config, ComponentInteractionCreatedEventArgs? result, CommandContext? context, bool logAndNotify = true)
        {
            if (result is null && context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.User;
            DiscordGuild guild = context?.Guild ?? result!.Guild;

            DiscordMessage? suggestionMessage =  result?.Message ?? await TryGetSuggestionMessage(question, config, guild);

            using (AppDbContext dbContext = new())
            {
                Question? modifyQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (modifyQuestion == null)
                {
                    DiscordEmbed embed = GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found in profile *{config.ProfileName}*.");

                    if (suggestionMessage is not null)
                    {
                        await suggestionMessage.ModifyAsync(embed: embed);
                        await suggestionMessage.UnpinAsync();
                    }

                    if (context is not null)
                        await context.Channel.SendMessageAsync(embed);
                    else
                        await result!.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
                    return;
                }

                modifyQuestion.Type = QuestionType.Accepted;
                modifyQuestion.AcceptedByUserId = user.Id;
                modifyQuestion.AcceptedTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                question = modifyQuestion;
            }

            string embedBody = General.GetSuggestionEmbedBody(question);
            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                DiscordEmbedBuilder editEmbed = GenericEmbeds.Custom($"{config.QotdShorthandText} Suggestion Accepted", embedBody +
                    $"\n\nAccepted by: {user.Mention}", color: "#20ff20");
                
                if (question.ThumbnailImageUrl is not null)
                {
                    editEmbed.WithThumbnail(question.ThumbnailImageUrl);
                }

                messageBuilder.AddEmbed(editEmbed);

                if (result is not null)
                {
                    await result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
                }
                else
                {
                    await suggestionMessage.ModifyAsync(messageBuilder);
                }

                if (config.EnableSuggestionsPinMessage)
                    await suggestionMessage.UnpinAsync();
            }

            if (!logAndNotify)
                return;

            DiscordEmbed responseEmbed = GenericEmbeds.Success($"Successfully Accepted {config.QotdShorthandText} Suggestion", embedBody);

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
                await Logging.Api.LogUserActionAsync(result!.Interaction.Channel, user, config, "Accepted Suggestion", question.ToString());
            else
                await Logging.Api.LogUserActionAsync(context, config, "Accepted Suggestion", question.ToString());
        }

        public static async Task DenySuggestionAsync(Question question, Config config, ModalSubmittedEventArgs? result, CommandContext? context, string? reason, bool logAndNotify = true)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.Interaction.User;
            DiscordGuild guild = context?.Guild ?? result!.Interaction.Guild!;

            DiscordMessage? suggestionMessage = await General.TryGetSuggestionMessage(question, config, guild);

            using (AppDbContext dbContext = new())
            {
                Question? disableQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (disableQuestion == null)
                {                    
                    DiscordEmbed embed = GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found in profile *{config.ProfileName}*.");

                    if (suggestionMessage is not null)
                    {
                        await suggestionMessage.ModifyAsync(embed: embed);
                        await suggestionMessage.UnpinAsync();
                    }

                    if (context is not null)
                        await context.Channel.SendMessageAsync(embed);
                    else
                        await result!.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
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

            string embedBody = General.GetSuggestionEmbedBody(question);
            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                messageBuilder.AddEmbed(GenericEmbeds.Custom($"{config.QotdShorthandText} Suggestion Denied", embedBody +
                    $"\n\nDenied by: {user.Mention}{(!string.IsNullOrEmpty(reason) ? $"\nReason: \"**{reason}**\"" : "")}", color: "#ff2020"));

                await suggestionMessage.ModifyAsync(messageBuilder);
                if (config.EnableSuggestionsPinMessage)
                    await suggestionMessage.UnpinAsync();
            }

            if (!logAndNotify)
                return;

            DiscordEmbed responseEmbed = GenericEmbeds.Success($"Successfully Denied {config.QotdShorthandText} Suggestion", embedBody +
                (!string.IsNullOrEmpty(reason) ? $"\n\nReason: \"**{reason}**\"" : ""));

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
                        $"Has been :x: **DENIED** :x:{(!string.IsNullOrEmpty(reason) ? $" for the following reason:\n" +
                        $"\"**{reason}**\"" : ".")}",
                        color: "#ff2020"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            string logTitle = "Denied Suggestion" + (config.EnableDeletedToStash ? " (moved to stash)" : "");
            string logBody = $"{question}" + (!string.IsNullOrEmpty(reason) ?
                $"\n\nDenial Reason: \"**{reason}**\"" : "");

            if (context is null)
                await Logging.Api.LogUserActionAsync(result!.Interaction.Channel, user, config, "Denied Suggestion" + (config.EnableDeletedToStash ? " (moved to stash)" : ""), $"{question}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
            else
                await Logging.Api.LogUserActionAsync(context, config, logTitle, logBody);
        }

        
        /// <summary>
        /// Get the modal for denying a suggestion with reason input.
        /// </summary>
        public static DiscordModalBuilder GetSuggestionDenyModal(Config config, Question question)
        {
            return new DiscordModalBuilder()
                .WithTitle($"Denial of \"{Core.Helpers.General.TrimIfNecessary(question.Text!, 32)}\"")
                .WithCustomId($"suggestions-deny/{config.ProfileId}/{question.GuildDependentId}")
                .AddTextInput(label: "Denial Reason", input: new DiscordTextInputComponent(
                    customId: "reason", placeholder: "Add an optional denial reason that will be sent to the user.", max_length: 1024, required: false, style: DiscordTextInputStyle.Paragraph));
        }
    }
}
