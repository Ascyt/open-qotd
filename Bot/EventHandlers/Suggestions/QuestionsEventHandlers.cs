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
using OpenQotd.Helpers.Suggestions;

namespace OpenQotd.EventHandlers.Suggestions
{
    public class QuestionsEventHandlers
    {
        /// <summary>
        /// Get the modal for adding a new question.
        /// </summary>
        public static DiscordModalBuilder GetQuestionsAddModal(Config config)
        {
            return new DiscordModalBuilder()
                .WithTitle($"Add a new {config.QotdShorthandText}!")
                .WithCustomId($"questions-add/{config.ProfileId}")
                .AddSelectMenu(label:"Question Type", select: new DiscordSelectComponent(
                    customId: "type", placeholder:"Select...", options: 
                    Enum.GetValues<QuestionType>()
                        .Select(t => new DiscordSelectComponentOption(
                            label: $"{t}", 
                            value: ((int)t).ToString(), 
                            isDefault: t == QuestionType.Accepted,
                            emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(Program.Client, Question.GetEmoji(t)))))
                ))
                .AddTextInput(label: "Contents", input: new DiscordTextInputComponent(
                    customId: "text", placeholder: "The primary text body of the question.", max_length: Program.AppSettings.QuestionTextMaxLength, required: true, style: DiscordTextInputStyle.Paragraph))
                .AddTextInput(label: "(optional) Additional Information", input: new DiscordTextInputComponent(
                    customId: "notes", placeholder: $"There will be a button for people to view this info under the sent {config.QotdShorthandText}.", max_length: Program.AppSettings.QuestionNotesMaxLength, required: false, style: DiscordTextInputStyle.Paragraph))
                .AddTextInput(label: "(optional) Thumbnail (Image link)", input: new DiscordTextInputComponent(
                    customId: "thumbnail-url", placeholder: "Will be shown alongside the QOTD.", max_length: Program.AppSettings.QuestionThumbnailImageUrlMaxLength, required: false, style: DiscordTextInputStyle.Short))
                .AddTextInput(label: "(optional) Staff Info", input: new DiscordTextInputComponent(
                    customId: "suggester-adminonly", placeholder: "This will only be visible to staff when viewing the question.", max_length: Program.AppSettings.QuestionSuggesterAdminInfoMaxLength, required: false, style: DiscordTextInputStyle.Paragraph));
        }

        public static async Task QuestionsAddModalSubmitted(ModalSubmittedEventArgs args, int profileId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsAdmin(args, config))
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

            QuestionType type = (QuestionType)int.Parse(((SelectMenuModalSubmission)args.Values["type"]).Values[0]);

            string text = ((TextInputModalSubmission)args.Values["text"]).Value;
            string? notes = ((TextInputModalSubmission)args.Values["notes"]).Value;
            if (string.IsNullOrWhiteSpace(notes))
                notes = null;

            string? thumbnailUrl = ((TextInputModalSubmission)args.Values["thumbnail-url"]).Value;
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
                thumbnailUrl = null;

            string? suggesterAdminOnly = ((TextInputModalSubmission)args.Values["suggester-adminonly"]).Value;
            if (string.IsNullOrWhiteSpace(suggesterAdminOnly))
                suggesterAdminOnly = null;

            Question newQuestion = new()
            {
                ConfigId = config.Id,
                GuildId = config.GuildId,
                GuildDependentId = await Question.GetNextGuildDependentId(config),
                Type = type,
                Text = text,
                Notes = notes,
                ThumbnailImageUrl = thumbnailUrl,
                SuggesterAdminOnlyInfo = suggesterAdminOnly,
                SubmittedByUserId = args.Interaction.User.Id,
                Timestamp = DateTime.UtcNow
            };
            
            if (!await Question.CheckQuestionValidity(newQuestion, null, config))
                return;

            using (AppDbContext dbContext = new())
            {
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();
            }

            if (type == QuestionType.Suggested) 
            {
                // Send suggestion notification message
                await SuggestionsHelpers.TryResetSuggestionMessageIfEnabledAsync(newQuestion, config, args.Interaction.Guild!);
            }

            string body = newQuestion.ToString(longVersion: true);

			await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                 .AddEmbed(GenericEmbeds.Success("Added Question", body))
                );
            await Logging.LogUserAction(args.Interaction.Channel, args.Interaction.User, config, "Added Question", message: body);
        }
    }
}
