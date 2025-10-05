using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;
using System.ComponentModel;

namespace OpenQotd.Bot.Commands
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
            [Description("The feedback, suggestion or bug.")] string feedback)
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

        [Command("help")]
        [Description("Print general information about OpenQOTD")]
        public static async Task HelpAsync(CommandContext context,
            [Description("Which viewable OpenQOTD profile to view general information of.")][SlashAutoCompleteProvider<ViewableProfilesAutoCompleteProvider>] int? For=null)
        {
            Config? config = For is null ? await ProfileHelpers.TryGetDefaultConfigAsync(context) : await ProfileHelpers.TryGetConfigAsync(context, For.Value);
            if (config is null || !await CommandRequirements.UserIsBasic(context, config))
                return;

            string userRole = $"Basic {config.QotdShorthandText} User";
            if (context.Member!.Permissions.HasPermission(DiscordPermission.Administrator))
                userRole = "Full Server Administrator (incl. `/config` and `/presets`)";
            else if (await CommandRequirements.UserIsAdmin(context, config, responseOnError:false))
                userRole = $"{config.QotdShorthandText} Administrator (excl. `/config` and `/presets`)";

            string configValuesDescription = config == null ?
                $"**:warning: Config not initialized**" :
                $"- Is default profile: {config.IsDefaultProfile}\n" +
                $"- {config.QotdShorthandText} Title: *{config.QotdTitleText}*\n" + 
                $"- {config.QotdShorthandText} channel: <#{config.QotdChannelId}>\n" +
                $"- {config.QotdShorthandText} time: {DSharpPlus.Formatter.Timestamp(DateTime.Today + new TimeSpan(config.QotdTimeHourUtc, config.QotdTimeMinuteUtc, 0), DSharpPlus.TimestampFormat.ShortTime)}\n" +
                $"- Your role: **{userRole}**\n" +
                $"- Suggestions enabled: **{config.EnableSuggestions}**";

            DiscordEmbed responseEmbed = GenericEmbeds.Info(title: $"OpenQOTD v{Program.AppSettings.Version}", message:
                $"# About\n" +
                $"*OpenQOTD is a free and open-source bot that allows user-suggested, staff-added, or preset messages to be sent at regular intervals. " +
                $"It was originally meant to only be a \"Question Of The Day\"-bot, however it has evolved to allow for much more than that, with many more features planned.\n" +
                $"\n" +
                $"If you enjoy this bot, please consider [adding it to a server](<https://open-qotd.ascyt.com/add>) or joining the [Community & Support Server](<https://open-qotd.ascyt.com/community>). " +
                $"You can find the documentation and a little bit of extra info about the bot [here](<https://open-qotd.ascyt.com/>).\n" +
                $"\n" +
                $"I'm a young hobbyist developer, and, aside for the occasional donation, have not made a cent on this mostly solo project. " +
                $"If you enjoy this bot and would like to help out, please consider supporting me with a small [Donation](<https://ascyt.com/donate>) for the countless hours I've spent working on it, I would appreciate it a ton :)*\n" +
                $"\n" +
                $"# Basic Commands\n" +
                $"- `/qotd` or `/suggest`: Suggest a {config!.QotdShorthandText} to the current server if suggestions are enabled.\n" +
                $"- `/leaderboard` or `/lb`: View a learderboard ranked on the amount of questions sent.\n" +
                $"- `/topic`: Send a random already sent {config.QotdShorthandText} to the current channel, to revive a dead chat.\n" +
                $"- `/sentquestions`: View all {config.QotdShorthandText}'s that have been sent.\n" +
                $"- `/feedback`: Submit feedback, suggestions or bug reports to the developers of OpenQOTD.\n" +
                $"\n" +
                $"# Config & User Values\n" +
                $"{configValuesDescription}\n" +
                $"\n" +
                $"# Useful Links\n" +
                $"- :heart: [Donate](https://ascyt.com/donate/)\n" +
                $"- [Add OpenQOTD to your server!](https://open-qotd.ascyt.com/add)\n" +
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
