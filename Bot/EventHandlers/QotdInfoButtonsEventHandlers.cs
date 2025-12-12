using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Commands;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using Sprache;

namespace OpenQotd.EventHandlers
{
    /// <summary>
    /// Includes event handlers for show-qotd-notes and show-general-info.
    /// </summary>
    public class QotdInfoButtonsEventHandlers
    {
        /// <summary>
        /// Shows the notes for a QOTD question, if any exist.
        /// </summary>
        public static async Task ShowQotdNotesButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDependentId)
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
                await EventHandlers.RespondWithError(args, $"Question with ID `{questionGuildDependentId}` for profile \"{config.ProfileName}\" not found.");
                return;
            }

            if (question.Notes is null)
            {
                await EventHandlers.RespondWithError(args, "This question does not have any submittor-written additional notes.");
                return;
            }

            DiscordEmbed notesEmbed = GenericEmbeds.Info(title: $"Additional {config.QotdShorthandText} Information", message: question.Notes)
                .WithFooter($"Written by the submittor.");

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(notesEmbed)
                .AsEphemeral());
        }

        /// <summary>
        /// Shows general info about OpenQOTD. If this is a response to a question and the user is an admin, prompts the user to choose between question info and general info.
        /// </summary>
        public static async Task ShowGeneralInfoButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDependentId)
        {
            DiscordMember member = await args.Guild.GetMemberAsync(args.User.Id);

            Config? config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config, member: member))
                return;

            // If this is a response to a question and the user is an admin, show the prompt.
            if (questionGuildDependentId != -1 && await CommandRequirements.UserIsAdmin(args, config, responseOnError:false))
            {
                DiscordInteractionResponseBuilder promptMessage = new();
                promptMessage.AddEmbed(GenericEmbeds.Info(title: "Choose An Option",
                    message: $"Would you like to view information about this question or general information about OpenQOTD? Select below."));

                promptMessage.AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, $"edit_show-qotd-info/{profileId}/{questionGuildDependentId}", "Question Info"),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, $"edit_show-general-info-no-prompt/{profileId}", "About OpenQOTD"));

                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, promptMessage.AsEphemeral());
                return;
            }

            // Otherwise, show general info.
            await ShowGeneralInfoNoPromptButtonClicked(args, profileId, editMessage: false, config: config, member: member);
        }

        /// <summary>
        /// Shows general info about OpenQOTD (like /help) without prompting the user to choose between question info and general info.
        /// </summary>
        /// <param name="config">Optionally provided to avoid re-fetching</param>
        /// <param name="member">Optionally provided to avoid re-fetching</param>
        public static async Task ShowGeneralInfoNoPromptButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, bool editMessage, Config? config=null, DiscordMember? member=null)
        {
            member ??= await args.Guild.GetMemberAsync(args.User.Id);

            if (config is null)
            {
                config = await ProfileHelpers.TryGetConfigAsync(args, profileId);
                if (config is not null && !await CommandRequirements.UserIsBasic(args, config, member))
                    return;
            }

            await args.Interaction.CreateResponseAsync(editMessage ? DiscordInteractionResponseType.UpdateMessage : DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(await SimpleCommands.GetHelpEmbedAsync(config, args.Guild, member)).AsEphemeral());
        }

        /// <summary>
        /// Show info about a QOTD question (like /questions view).
        /// </summary>
        public static async Task ShowQotdInfoButtonClicked(ComponentInteractionCreatedEventArgs args, int profileId, int questionGuildDependentId, bool editMessage)
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
                await EventHandlers.RespondWithError(args, $"Question with ID `{questionGuildDependentId}` for profile \"{config.ProfileName}\" not found.");
                return;
            }

            await args.Interaction.CreateResponseAsync(editMessage ? DiscordInteractionResponseType.UpdateMessage : DiscordInteractionResponseType.ChannelMessageWithSource, 
                new DiscordInteractionResponseBuilder(QuestionsCommand.GetQuestionsViewResponse(config, question)).AsEphemeral());
        }
    }
}
