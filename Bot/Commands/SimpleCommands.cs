using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace OpenQotd.Bot.Commands
{
    public class SimpleCommands
    {
        [Command("sentquestions")]
        [Description("View all sent QOTDs")]
        public static async Task ViewSentQuestionsAsync(CommandContext context, 
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            if (!await CommandRequirements.UserIsBasic(context, null))
                return;

            await QuestionsCommand.ListQuestionsNoPermcheckAsync(context, Database.Entities.QuestionType.Sent, page);
        }

        const ulong FEEDBACK_SERVER_ID = 1281622725223514145;
        const ulong FEEDBACK_CHANNEL_ID = 1310316617032401018;
        [Command("feedback")]
        [Description("Leave feedback or suggestions or report bugs for the developers of OpenQOTD.")]
        public static async Task FeedbackAsync(CommandContext context,
            [Description("The feedback, suggestion or bug.")] string feedback)
        {
            string contents = $"[FEEDBACK] @{context!.User.Username} ({context!.User.Id}):\n\t{feedback}\n\n";

            await File.AppendAllTextAsync("feedback.txt", contents);

            await Console.Out.WriteAsync(contents);

            DiscordEmbed responseEmbed = GenericEmbeds.Success("OpenQOTD feedback sent!",
                    $"\"**{feedback}**\"");

            if (context is SlashCommandContext)
            {
                SlashCommandContext slashCommandcontext = (context as SlashCommandContext)!;

                await slashCommandcontext.RespondAsync(responseEmbed, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(responseEmbed);
            }


            DiscordChannel? feedbackChannel = await (await Program.Client.GetGuildAsync(FEEDBACK_SERVER_ID))
                .GetChannelAsync(FEEDBACK_CHANNEL_ID);

            if (feedbackChannel is not null)
            {
                await feedbackChannel.SendMessageAsync(GenericEmbeds.Custom(title: "New Feedback", message:
                    $"**{feedback}**\n\n" +
                    $"*Submitted by {context.User.Mention} in \"{context.Guild!.Name}\"*"));
            }
        }

        [Command("help")]
        [Description("Print general information about OpenQOTD")]
        public static async Task HelpAsync(CommandContext context)
        {
            Config? config = await CommandRequirements.TryGetConfig(context);

            if (config is null || !await CommandRequirements.UserIsBasic(context, config))
                return;

            string userRole = "Basic User";
            if (context.Member!.Permissions.HasPermission(DiscordPermission.Administrator))
                userRole = "Full Administrator (incl. Config)";
            else if (await CommandRequirements.UserIsAdmin(context, config, responseOnError:false))
                userRole = "QOTD Administrator (excl. Config)";

            string configValuesDescription = config == null ?
                $"**:warning: Config not initialized**" :
                $"- User role: **{userRole}**\n" +
                $"- QOTD channel: <#{config.QotdChannelId}>\n" +
                $"- QOTD time: {DSharpPlus.Formatter.Timestamp(DateTime.Today + new TimeSpan(config.QotdTimeHourUtc, config.QotdTimeMinuteUtc, 0), DSharpPlus.TimestampFormat.ShortTime)}\n" +
                $"- Suggestions enabled: **{config.EnableSuggestions}**";

            DiscordEmbed responseEmbed = GenericEmbeds.Custom($"OpenQOTD v{Program.AppSettings.Version} - Help", 
                $"*OpenQOTD is an open-source Question Of The Day Discord bot with a strong focus on a random sending of QOTDs, custom questions, suggestions, presets and more.*\n" +
                $"# Basic Commands\n" +
                $"- `/qotd` or `/suggest`: Suggest a QOTD to the current server if suggestions are enabled.\n" +
                $"- `/leaderboard` or `/lb`: View a learderboard based on whose questions have been sent the most.\n" +
                $"- `/topic`: Send a random question to the current channel, to revive a dead chat.\n" +
                $"- `/sentquestions`: View all questions that have been sent.\n" +
                $"- `/feedback`: Submit feedback, suggestions or bug reports to the developers of OpenQOTD.\n" +
                $"# Server & User values\n" +
                $"{configValuesDescription}\n" +
                $"# Useful Links\n" +
                $"- :heart: [Donate](https://ascyt.com/donate/)\n" +
                $"- [Documentation & About](https://open-qotd.ascyt.com/)\n" +
                $"- [Community & Support Server](https://open-qotd.ascyt.com/community)\n" +
                $"\n" +
                $"- [Source Code (GitHub)](https://github.com/Ascyt/open-qotd)\n" +
                $"- [About the Creator](https://ascyt.com/)\n" +
                $"\n" +
                $"- [Terms of Service](https://open-qotd.ascyt.com/terms-of-service)\n" +
                $"- [Privacy Policy](https://open-qotd.ascyt.com/privacy-policy)\n"
                );

            if (context is SlashCommandContext)
            {
                SlashCommandContext slashCommandcontext = (context as SlashCommandContext)!;

                await slashCommandcontext.RespondAsync(responseEmbed, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(responseEmbed);
            }
        }
    }
}
