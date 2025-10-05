using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;

namespace OpenQotd.Bot.EventHandlers
{
    /// <summary>
    /// Includes event handlers for show-qotd-notes and show-general-info.
    /// </summary>
    public class QotdInfoButtonsEventHandlers
    {
        public static async Task ShowQotdNotesButtonClicked(DiscordClient client, ComponentInteractionCreatedEventArgs args, int profileId, int guildDependentId)
        {
            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == guildDependentId)
                    .FirstOrDefaultAsync();
            }
            if (question is null)
            {
                await EventHandlers.RespondWithError(args, $"Question with ID `{guildDependentId}` for profile \"{config.ProfileName}\" not found.");
                return;
            }

            if (question.Notes is null)
            {
                await EventHandlers.RespondWithError(args, "This question does not have any submittor-written additional notes.");
                return;
            }

            DiscordEmbed notesEmbed = GenericEmbeds.Info(title:$"Additional {config.QotdShorthandText} Information", message: question.Notes)
                .WithFooter($"Written by the submittor.");

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(notesEmbed)
                .AsEphemeral());
        }
    }
}
