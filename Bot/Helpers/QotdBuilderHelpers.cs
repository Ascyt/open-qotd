using DSharpPlus.Entities;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.Helpers
{
    internal static class QotdBuilderHelpers
    {
        /// <summary>
        /// Get the modal for suggesting a new QOTD.
        /// </summary>
        public static DiscordInteractionResponseBuilder GetQotdModal(Config config, string guildName)
        {
            // TODO: Max length for some of these in appsettings
            return new DiscordInteractionResponseBuilder()
                .WithTitle($"Suggest a new {config.QotdShorthandText}!")
                .WithCustomId($"suggest-qotd/{config.ProfileId}")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Contents", customId: "text", placeholder: $"This will require approval from the staff of \"{GeneralHelpers.TrimIfNecessary(guildName, 52)}\".", max_length: Program.AppSettings.QuestionTextMaxLength, required: true, style: DiscordTextInputStyle.Paragraph))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Additional Information", customId: "notes", placeholder: $"There will be a button for people to view this info under the sent {config.QotdShorthandText}.", max_length: 1000, required: false, style: DiscordTextInputStyle.Paragraph))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Thumbnail (Image link)", customId: "thumbnail-url", placeholder: "Only image URLs from Discord or Imgur are allowed.", max_length: 1024, required: false, style: DiscordTextInputStyle.Short))
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "(optional) Staff Info", customId: "suggester-adminonly", placeholder: "This will only be visible to staff for reviewing the suggestion.", max_length: 1000, required: false, style: DiscordTextInputStyle.Paragraph));
        }
    }
}
