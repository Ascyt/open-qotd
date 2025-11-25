using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Commands;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;

namespace OpenQotd.EventHandlers.Suggestions
{
    public class CreateSuggestionEventHandlers
    {
        public static async Task SuggestQotdButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            if (!config.EnableSuggestions)
            {
                await EventHandlers.RespondWithError(args, $"Suggestions are not enabled for this profile ({config.ProfileName}).");
                return;
            }

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, GetQotdModal(config, args.Guild.Name));
        }

        /// <summary>
        /// Get the modal for suggesting a new QOTD.
        /// </summary>
        public static DiscordInteractionResponseBuilder GetQotdModal(Config config, string guildName)
        {
            return new DiscordInteractionResponseBuilder()
                .WithTitle($"Suggest a new {config.QotdShorthandText}!")
                .WithCustomId($"suggest-qotd/{config.ProfileId}")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Contents", customId: "text", placeholder: $"This will require approval from the staff of \"{GeneralHelpers.TrimIfNecessary(guildName, 52)}\".", max_length: Program.AppSettings.QuestionTextMaxLength, required: true, style: DiscordTextInputStyle.Paragraph))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Additional Information", customId: "notes", placeholder: $"There will be a button for people to view this info under the sent {config.QotdShorthandText}.", max_length: Program.AppSettings.QuestionNotesMaxLength, required: false, style: DiscordTextInputStyle.Paragraph))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Thumbnail (Image link)", customId: "thumbnail-url", placeholder: "Will be shown alongside the QOTD.", max_length: Program.AppSettings.QuestionThumbnailImageUrlMaxLength, required: false, style: DiscordTextInputStyle.Short))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Staff Info", customId: "suggester-adminonly", placeholder: "This will only be visible to staff for reviewing the suggestion.", max_length: Program.AppSettings.QuestionSuggesterAdminInfoMaxLength, required: false, style: DiscordTextInputStyle.Paragraph));
        }

        public static async Task SuggestQotdModalSubmitted(ModalSubmittedEventArgs args, int profileId)
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
                DiscordEmbed errorEmbed = GenericEmbeds.Error($"Suggestions are not enabled for this profile ({config.ProfileName}).");

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .AddEmbed(errorEmbed)
                    .AsEphemeral());
                return;
            }

            string text = args.Values["text"];
            string? notes = args.Values["notes"];
            if (string.IsNullOrWhiteSpace(notes))
                notes = null;

            string? thumbnailUrl = args.Values["thumbnail-url"];
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
                thumbnailUrl = null;

            string? suggesterAdminOnly = args.Values["suggester-adminonly"];
            if (string.IsNullOrWhiteSpace(suggesterAdminOnly))
                suggesterAdminOnly = null;

            Question newQuestion = new()
            {
                ConfigId = config.Id,
                GuildId = config.GuildId,
                GuildDependentId = await Question.GetNextGuildDependentId(config),
                Type = QuestionType.Suggested,
                Text = text,
                Notes = notes,
                ThumbnailImageUrl = thumbnailUrl,
                SuggesterAdminOnlyInfo = suggesterAdminOnly,
                SubmittedByUserId = args.Interaction.User.Id,
                Timestamp = DateTime.UtcNow
            };
            (bool, DiscordEmbed) result = await SuggestAsync(newQuestion, config, args.Interaction.Guild!, args.Interaction.Channel, args.Interaction.User);

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(result.Item2);
            DiscordInteractionResponseBuilder responseBuilder = new(messageBuilder)
            {
                IsEphemeral = true
            };

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);
        }

        /// <returns>(whether or not successful, response message)</returns>
        public static async Task<(bool, DiscordEmbed)> SuggestAsync(Question newQuestion, Config config, DiscordGuild guild, DiscordChannel channel, DiscordUser user)
        {
            if (!await Question.CheckQuestionValidity(newQuestion, null, config))
                return (false, GenericEmbeds.Error("Text validity check failed"));

            using (AppDbContext dbContext = new())
            {
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();
            }

            (bool, DiscordEmbedBuilder) result = (true, GenericEmbeds.Success($"{config.QotdShorthandText} Suggested!",
                    $"Your **{config.QotdTitleText}** suggestion:\n" +
                    $"\"{GeneralHelpers.Italicize(newQuestion.Text!)}\"\n" +
                    $"\n" +
                    $"Has successfully been suggested!\n" +
                    $"You will be notified when it gets accepted or denied."));

            if (newQuestion.ThumbnailImageUrl is not null)
                result.Item2.WithThumbnail(newQuestion.ThumbnailImageUrl);

            await Logging.LogUserAction(channel, user, config,
                title: $"Suggested {config.QotdShorthandText}",
                message: $"\"**{newQuestion.Text}**\"\nID: `{newQuestion.GuildDependentId}`");

            if (config.SuggestionsChannelId is null)
            {
                if (config.SuggestionsPingRoleId is not null)
                {
                    await channel.SendMessageAsync(GenericEmbeds.Warning("Suggestions ping role is set, but suggestions channel is not.\n\n" +
                        "*The channel can be set using `/config set suggestions_channel [channel]`, or the ping role can be removed using `/config reset suggestions_ping_role`.*"));
                }
                return result;
            }

            DiscordRole? pingRole = null;
            if (config.SuggestionsPingRoleId is not null)
            {
                try
                {
                    pingRole = await guild.GetRoleAsync(config.SuggestionsPingRoleId.Value);
                }
                catch (NotFoundException)
                {
                    await channel.SendMessageAsync(
                        GenericEmbeds.Warning("Suggestions ping role is set, but not found.\n\n" +
                        "*It can be set using `/config set suggestions_ping_role [channel]`, or unset using `/config reset suggestions_ping_role`.*")
                        );
                }
            }

            DiscordChannel suggestionsChannel;
            try
            {
                suggestionsChannel = await guild.GetChannelAsync(config.SuggestionsChannelId!.Value);
            }
            catch (NotFoundException)
            {
                return (false,
                    GenericEmbeds.Warning("Suggestions channel is set, but not found.\n\n" +
                    "*It can be set using `/config set suggestions_channel [channel]`, or unset using `/config reset suggestions_channel`.*")
                    );
            }

            DiscordMessageBuilder messageBuilder = new();

            AddPingIfAvailable(messageBuilder, pingRole);

            string embedBody = $"**Contents:**\n" +
                $"\"{GeneralHelpers.Italicize(newQuestion.Text!)}\"\n" +
                $"\n" +
                $"By: {user.Mention} (`{user.Id}`)\n" +
                $"ID: `{newQuestion.GuildDependentId}`" +
                (newQuestion.ThumbnailImageUrl is not null ? $"\nIncludes a thumbnail image (if it's not visible in this message, it means that the fetching for it failed)." : null);

            DiscordEmbedBuilder availableEmbed = GenericEmbeds.Custom(title: $"A new {config.QotdShorthandText} Suggestion is available!", message: embedBody,
                color: "#f0b132");

            if (newQuestion.ThumbnailImageUrl is not null)
            {
                availableEmbed.WithThumbnail(newQuestion.ThumbnailImageUrl);
            }

            messageBuilder.AddEmbed(availableEmbed);

            if (newQuestion.Notes is not null)
            {
                messageBuilder.AddEmbed(
                    GenericEmbeds.Info(title: "Additional Information", message: GeneralHelpers.Italicize(newQuestion.Notes))
                    .WithFooter("Written by the suggester, visible to everyone.")
                    );
            }

            if (newQuestion.SuggesterAdminOnlyInfo is not null)
            {
                messageBuilder.AddEmbed(
                    GenericEmbeds.Info(title: "Staff Note", message: GeneralHelpers.Italicize(newQuestion.SuggesterAdminOnlyInfo))
                    .WithFooter("Written by the suggester, only visible to staff.")
                    );
            }

            messageBuilder.AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, $"suggestions-accept/{config.ProfileId}/{newQuestion.GuildDependentId}", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"suggestions-deny/{config.ProfileId}/{newQuestion.GuildDependentId}", "Deny")
            );

            DiscordMessage message = await suggestionsChannel.SendMessageAsync(
                    messageBuilder
                );

            if (config.EnableSuggestionsPinMessage)
                await message.PinAsync();

            using (AppDbContext dbContext = new())
            {
                Question? updateQuestion = await dbContext.Questions.FindAsync(newQuestion.Id);

                if (updateQuestion != null)
                {
                    updateQuestion.SuggestionMessageId = message.Id;

                    await dbContext.SaveChangesAsync();

                    newQuestion = updateQuestion;
                }
            }

            return result;
        }
        private static void AddPingIfAvailable(DiscordMessageBuilder messageBuilder, DiscordRole? pingRole)
        {
            if (pingRole is not null)
            {
                messageBuilder.WithContent(pingRole.Mention);
                messageBuilder.WithAllowedMention(new RoleMention(pingRole));
            }
        }
    }
}
