using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using OpenQotd.QotdSending;
using System.ComponentModel;

namespace OpenQotd.Commands
{
    public class SimpleCommands
    {
        [Command("sentquestions")]
        [Description("View all sent QOTDs")]
        public static async Task ViewSentQuestionsAsync(CommandContext context,
            [Description("Which OpenQOTD profile to consider.")][SlashAutoCompleteProvider<ViewableProfilesAutoCompleteProvider>] int of,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            int profileId = of;

            Config? config = await ProfileHelpers.TryGetConfigAsync(context, profileId);
            if (config is null || !await CommandRequirements.UserIsBasic(context, config))
                return;

            await QuestionsCommand.ListQuestionsNoPermcheckAsync(context, config, QuestionType.Sent, page);
        }

        [Command("feedback")]
        [Description("Leave feedback or suggestions or report bugs for the developers of OpenQOTD.")]
        public static async Task FeedbackAsync(CommandContext context,
            [Description("The feedback, suggestion or bug for the OpenQOTD Discord Bot.")] string feedback)
        {
            if (Program.AppSettings.FeedbackBlockedUserIds.Contains(context.User.Id))
            {
                await (context as SlashCommandContext)!
                    .RespondAsync(
                        GenericEmbeds.Error(title: "Forbidden", message: "You have been blocked from suggesting feedback with `/feedback` or `/presets suggest`."),
                        ephemeral: true);
                return;
            }

            string contents = $"[FEEDBACK] @{context!.User.Username} ({context!.User.Id}):\n\t{feedback}\n\n";

            await File.AppendAllTextAsync("feedback.txt", contents);

            await Console.Out.WriteAsync(contents);

            DiscordEmbed responseEmbed = GenericEmbeds.Success("OpenQOTD feedback sent!",
                    $"\"**{feedback}**\"\n" + 
                    "\n" +
                    "Thank you so much for helping to improve OpenQOTD! For any questions or follow-ups, please join the [Community & Support Server](<https://open-qotd.ascyt.com/community>) or DM <@417669404537520128>/`@ascyt`.");

            if (context is SlashCommandContext)
            {
                SlashCommandContext slashCommandcontext = (context as SlashCommandContext)!;

                await slashCommandcontext.RespondAsync(responseEmbed, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(responseEmbed);
            }


            DiscordChannel? feedbackChannel = await (await Program.Client.GetGuildAsync(Program.AppSettings.FeedbackGuildId))
                .GetChannelAsync(Program.AppSettings.FeedbackChannelId);

            if (feedbackChannel is not null)
            {
                await feedbackChannel.SendMessageAsync(GenericEmbeds.Info(title: "New Feedback", message:
                    $"**{feedback}**\n\n" +
                    $"*Submitted by {context.User.Mention} in \"{context.Guild!.Name}\"*")
                    .WithFooter($"User ID: {context.User.Id}"));
            }
        }
    }
}
