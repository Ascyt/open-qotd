using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.Helpers
{
    internal static class GeneralHelpers
    {
        /// <summary>
        /// Gets a channel from an ID.
        /// </summary>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordChannel?> GetDiscordChannel(ulong id, DiscordGuild? guild = null, ulong? guildId = null, CommandContext? commandContext = null)
        {
            if (guildId is null && commandContext is null && guild is null)
                throw new ArgumentNullException(nameof(guildId));

            try
            {
                if (guild is null)
                {
                    DiscordGuild? actualGuild = (commandContext is not null) ?
                        commandContext.Guild : await Program.Client.GetGuildAsync(guildId!.Value);

                    if (actualGuild is null)
                        return null;

                    return await actualGuild.GetChannelAsync(id);
                }
                return await guild.GetChannelAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a channel from an ID.
        /// </summary>
        /// <returns>The channel, or null if it's not found</returns>
        public static async Task<DiscordMessage?> GetDiscordMessage(ulong id, DiscordChannel channel)
        {
            try
            {
                return await channel.GetMessageAsync(id);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the modal for denying a suggestion with reason input.
        /// </summary>
        public static DiscordInteractionResponseBuilder GetSuggestionDenyModal(Config config, Question question)
        {
            return new DiscordInteractionResponseBuilder()
                .WithTitle($"Denial of \"{TrimIfNecessary(question.Text!, 32)}\"")
                .WithCustomId($"suggestions-deny/{config.ProfileId}/{question.GuildDependentId}")
                .AddTextInputComponent(new DiscordTextInputComponent(
                    label: "Denial Reason", customId: "reason", placeholder: "This will be sent to the user.", max_length: 1024, required: true, style: DiscordTextInputStyle.Paragraph));
        }

        /// <summary>
        /// Trim text if it exceeds maxLength, adding an ellipsis if so.
        /// </summary>
        public static string TrimIfNecessary(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;
            return text[..(maxLength - 1)] + "…";
        }
    }
}
