using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using OpenQotd.QotdSending;
using System.ComponentModel;

namespace OpenQotd.Commands
{
    public class HelpCommand
    {
        [Command("help")]
        [Description("Print general information about OpenQOTD")]
        public static async Task HelpAsync(CommandContext context,
            [Description("Which viewable OpenQOTD profile to view general information of.")][SlashAutoCompleteProvider<ViewableProfilesAutoCompleteProvider>] int? For=null)
        {
            SlashCommandContext slashCommandContext = (context as SlashCommandContext)!;
            await slashCommandContext.DeferResponseAsync(ephemeral: true);

            await slashCommandContext.FollowupAsync(await GetHelpMessageWithProfileSelectAsync(context.Guild!, context.Member!, For));
        }

        public static async Task<DiscordMessageBuilder> GetHelpMessageWithProfileSelectAsync(DiscordGuild guild, DiscordMember member, int? profileId)
        {
            Config? config = profileId is null ? 
                (await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(guild.Id, member.Id)).Item1 : 
                await ProfileHelpers.TryGetConfigAsync(guild.Id, profileId.Value);

            DiscordMessageBuilder messageBuilder = await GetHelpMessageAsync(config, guild, member);

            Dictionary<int, string> viewableProfiles = await ViewableProfilesAutoCompleteProvider.GetViewableProfilesAsync(guild, member, null);
            if (viewableProfiles.Count > 1)
            {
                messageBuilder.AddActionRowComponent(
                    new DiscordSelectComponent(
                        "help-select-profile",
                        "Select Profile...",
                        viewableProfiles
                            .Select(kv => new DiscordSelectComponentOption(
                                label: kv.Value,
                                value: kv.Key.ToString(),
                                isDefault: config is not null && config.ProfileId == kv.Key
                        ))
                    )
                );
            }

            return messageBuilder;
        }

        public static async Task OnProfileSelectChanged(ComponentInteractionCreatedEventArgs args)
        {
            int selectedProfileId = int.Parse(args.Values[0]);

            Config? config = await ProfileHelpers.TryGetConfigAsync(args, selectedProfileId);
            if (config is null || !await CommandRequirements.UserIsBasic(args, config))
                return;

            DiscordMember member = await args.Guild.GetMemberAsync(args.User.Id);

            DiscordMessageBuilder messageBuilder = await GetHelpMessageWithProfileSelectAsync(args.Interaction.Guild!, member, selectedProfileId);

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(messageBuilder).AsEphemeral());
        }

        public static async Task<DiscordMessageBuilder> GetHelpMessageAsync(Config? config, DiscordGuild guild, DiscordMember member)
        {
            DiscordMessageBuilder messageBuilder = new();
            if (config is not null) // if config is set, require basic perms
            {
                (bool userIsBasic, string? error) = await CommandRequirements.UserIsBasic(guild, member, config);

                if (!userIsBasic) 
                {
                    messageBuilder.AddEmbed(GenericEmbeds.Error(error!));
                    return messageBuilder;
                }
            }

            string configValuesDescription;
            if (config is not null) 
            {
                string userRole = $"Basic {config.QotdShorthandText} User";
                if (member.Permissions.HasPermission(DiscordPermission.Administrator))
                    userRole = "Full Server Administrator (incl. `/config` and `/profiles`)";
                else if ((await CommandRequirements.UserIsAdmin(guild, member, config)).Item1)
                    userRole = $"{config.QotdShorthandText} Administrator (excl. `/config` and `/profiles`)";

                DateTime? nextQotdTime = await QotdSenderTimer.GetConfigNextSendTime(config.Id);

                configValuesDescription = config == null ?
                    $"**:warning: Config not initialized**" :
                    $"- Is default profile: {config.IsDefaultProfile}\n" +
                    $"- {config.QotdShorthandText} title: *{config.QotdTitleText}*\n" +
                    $"- {config.QotdShorthandText} channel: <#{config.QotdChannelId}>\n" +
                    $"- {config.QotdShorthandText} time: {DSharpPlus.Formatter.Timestamp(DateTime.Today + new TimeSpan(config.QotdTimeHourUtc, config.QotdTimeMinuteUtc, 0), DSharpPlus.TimestampFormat.ShortTime)}\n" +
                    $"- {config.QotdShorthandText} day condition: {(string.IsNullOrWhiteSpace(config.QotdTimeDayCondition) ? "*daily*" : $"`{config.QotdTimeDayCondition}`")}\n" +
                    $"- Next {config.QotdShorthandText} will be sent at: {(nextQotdTime is null ? "*disabled*" : DSharpPlus.Formatter.Timestamp(nextQotdTime.Value, DSharpPlus.TimestampFormat.LongDateTime))}\n" +
                    $"- Suggestions enabled: **{config.EnableSuggestions}**\n" +
                    $"- Presets enabled: **{config.EnableQotdAutomaticPresets}**\n" +
                    $"- Your role: **{userRole}**";
            }
            else // no config initialized
            {
                configValuesDescription = "**:warning: The config has not been initialized yet. :warning:**\n" +
                    "\n" +
                    "Use `/config initialize` to initialize the config, and check out the [documentation](<https://open-qotd.ascyt.com/>) for full specifications.\n" +
                    "If you are still having issues, please join the [Community & Support Server](<https://open-qotd.ascyt.com/community>) or send me a DM (<@417669404537520128>/`@ascyt`) for help.";
            }

            messageBuilder.AddEmbed(GenericEmbeds.Info(title: $"OpenQOTD v{Program.AppSettings.Version}", message:
                $"# About\n" +
                $"*OpenQOTD is a free and open-source bot that allows user-suggested, staff-added, or preset messages to be sent at regular intervals. " +
                $"It was originally meant to only be a \"Question Of The Day\"-bot, however it has evolved to allow for much more than that, with many more features planned.\n" +
                $"\n" +
                $"If you enjoy this bot, please consider [adding it to a server](<https://open-qotd.ascyt.com/add>) or joining the [Community & Support Server](<https://open-qotd.ascyt.com/community>). " +
                $"You can find the documentation and a little bit of extra info about the bot [here](<https://open-qotd.ascyt.com/>).\n" +
                $"\n" +
                $"I'm a young hobbyist developer, and, aside for the occasional donation, have not made a cent on this mostly solo project. " +
                $"If you enjoy this bot and would like to help out, please consider supporting me with a small [Donation](<https://ascyt.com/donate>) for the countless hours I've spent working on it, I would appreciate it a ton :)*\n" +
                $"\n" + (config is not null ?
                $"# Basic Commands\n" +
                $"- `/qotd` or `/suggest`: Suggest a {config!.QotdShorthandText} to the current server if suggestions are enabled.\n" +
                $"- `/leaderboard` or `/lb`: View a learderboard ranked on the amount of questions sent.\n" +
                $"- `/topic`: Send a random already sent {config.QotdShorthandText} to the current channel, to revive a dead chat.\n" +
                $"- `/sentquestions`: View all {config.QotdShorthandText}'s that have been sent.\n" +
                $"- `/feedback`: Submit feedback, suggestions or bug reports to the developers of OpenQOTD.\n" +
                $"\n" : "") +
                $"# Config & User Values\n" +
                $"{configValuesDescription}\n" +
                $"\n" +
                $"# Useful Links\n" +
                $"- :heart: [Donate](https://ascyt.com/donate/) | [Vote](https://open-qotd.ascyt.com/vote) :heart:\n" +
                $"- [Add OpenQOTD to your server!](https://open-qotd.ascyt.com/add)\n" +
                $"- [Documentation & About](https://open-qotd.ascyt.com/)\n" +
                $"- [Community & Support Server](https://open-qotd.ascyt.com/community)\n" +
                $"\n" +
                $"- [Source Code (GitHub)](https://github.com/Ascyt/open-qotd)\n" +
                $"- [About the Creator](https://ascyt.com/)\n" +
                $"\n" +
                $"- [Terms of Service](https://open-qotd.ascyt.com/terms-of-service)\n" +
                $"- [Privacy Policy](https://open-qotd.ascyt.com/privacy-policy)\n"
                ));

            messageBuilder.AddActionRowComponent(
                new DiscordLinkButtonComponent(
                    url: "https://discord.com/oauth2/authorize?client_id=1275472589375930418",
                    label: "Add To Server",
                    emoji: new DiscordComponentEmoji("💠")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://open-qotd.ascyt.com/documentation",
                    label: "Documentation",
                    emoji: new DiscordComponentEmoji("🧾")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://discord.com/invite/85TtrwuKn8",
                    label: "Support Server",
                    emoji: new DiscordComponentEmoji("💬")
                ),
                new DiscordLinkButtonComponent(
                    url: "https://ascyt.com/donate",
                    label: "Donate",
                    emoji: new DiscordComponentEmoji("❤️")
                )
            );

            return messageBuilder;
        }
    }
}
