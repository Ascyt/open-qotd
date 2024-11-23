using CommunityToolkit.HighPerformance.Helpers;
using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace CustomQotd.Features.Commands
{
    public class SuggestCommand
    {
        [Command("suggest")]
        [Description("Suggest a Question Of The Day to be added.")]
        public static async Task SuggestAsync(CommandContext context,
            [Description ("The question to be added.")] string question)
        {
            if (!await CommandRequirements.IsConfigInitialized(context) || !await CommandRequirements.UserIsBasic(context))
                return;

            using (var dbContext = new AppDbContext())
            {
                Config? config = await dbContext.Configs.Where(c => c.GuildId == context.Guild!.Id).FirstOrDefaultAsync();

                if (config != null && !config.EnableSuggestions)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Suggestions Disabled", message: "Suggesting of QOTDs using `/qotd` or `/suggest` has been disabled by staff."));
                    return;
                }
            }

            (bool, DiscordEmbed) result = await SuggestNoContextAsync(question, context.Guild!, context.Channel, context.User);

            if (context is SlashCommandContext && result.Item1) // Is slash command and not errored
            {
                SlashCommandContext? slashCommandcontext = context as SlashCommandContext;

                await slashCommandcontext!.RespondAsync(result.Item2, ephemeral: true);
            }
            else
            {
                await context.RespondAsync(result.Item2);
            }
        }

        /// <returns>(whether or not successful, response message)</returns>
        public static async Task<(bool, DiscordEmbed)> SuggestNoContextAsync(string question, DiscordGuild guild, DiscordChannel discordChannel, DiscordUser user)
        {
            if (!await Question.CheckTextValidity(question, null))
                return (false, MessageHelpers.GenericErrorEmbed("Text validity check failed"));

            ulong guildId = guild.Id;
            ulong userId = user.Id;

            Question newQuestion;
            ulong? suggestionsChannelId;
            ulong? suggestionsPingRoleId;
            using (var dbContext = new AppDbContext())
            {
                newQuestion = new Question()
                {
                    GuildId = guildId,
                    GuildDependentId = await Question.GetNextGuildDependentId(guildId),
                    Type = QuestionType.Suggested,
                    Text = question,
                    SubmittedByUserId = userId,
                    Timestamp = DateTime.UtcNow
                };
                await dbContext.Questions.AddAsync(newQuestion);
                await dbContext.SaveChangesAsync();

                suggestionsChannelId = await dbContext.Configs.Where(c => c.GuildId == guildId).Select(c => c.SuggestionsChannelId).FirstOrDefaultAsync();
                suggestionsPingRoleId = await dbContext.Configs.Where(c => c.GuildId == guildId).Select(c => c.SuggestionsPingRoleId).FirstOrDefaultAsync();
            }

            var result = (true, MessageHelpers.GenericSuccessEmbed("QOTD Suggested!",
                    $"Your Question Of The Day:\n" +
                    $"> \"**{newQuestion.Text}**\"\n" +
                    $"\n" +
                    $"Has successfully been suggested!\n" +
                    $"You will be notified when it gets accepted or denied."));


            await Logging.LogUserAction(guildId, discordChannel, user, "Suggested QOTD", $"> \"**{newQuestion.Text}**\"\nID: `{newQuestion.GuildDependentId}`");

            if (suggestionsChannelId == null)
            {
                if (suggestionsPingRoleId == null)
                {
                    await discordChannel.SendMessageAsync(MessageHelpers.GenericWarningEmbed("Suggestions ping role is set, but suggestions channel is not.\n\n" +
                        "*The channel can be set using `/config set suggestions_channel [channel]`, or the ping role can be removed using `/config reset suggestions_ping_role`.*"));
                }
                // Qotd suggested but no suggestion channel
                return result;
            }

            DiscordRole? pingRole = null;
            if (suggestionsPingRoleId != null)
            {
                try
                {
                    pingRole = await guild.GetRoleAsync(suggestionsPingRoleId.Value);
                }
                catch (NotFoundException)
                {
                    await discordChannel.SendMessageAsync(
                        MessageHelpers.GenericWarningEmbed("Suggestions ping role is set, but not found.\n\n" +
                        "*It can be set using `/config set suggestions_ping_role [channel]`, or unset using `/config reset suggestions_ping_role`.*")
                        );
                }
            }

            DiscordChannel channel;
            try
            {
                channel = await guild.GetChannelAsync(suggestionsChannelId.Value);
            }
            catch (NotFoundException)
            {
                return (false,
                    MessageHelpers.GenericWarningEmbed("Suggestions channel is set, but not found.\n\n" +
                    "*It can be set using `/config set suggestions_channel [channel]`, or unset using `/config reset suggestions_channel`.*")
                    );
            }

            DiscordMessageBuilder messageBuilder = new();

            AddPingIfAvailable(messageBuilder, pingRole);

            string embedBody = $"> \"**{newQuestion.Text}**\"\n" +
                $"By: {user.Mention} (`{user.Id}`)\n" +
                $"ID: `{newQuestion.GuildDependentId}`";

            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed("A new QOTD Suggestion is available!", embedBody,
                color: "#f0b132"));

            messageBuilder.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Success, $"suggestions-accept/{newQuestion.GuildDependentId}", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"suggestions-deny/{newQuestion.GuildDependentId}", "Deny")
            );

            DiscordMessage message = await channel.SendMessageAsync(
                    messageBuilder
                );

            await message.PinAsync();

            using (var dbContext = new AppDbContext())
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


        [Command("qotd")]
        [Description("Suggest a Question Of The Day to be added.")]
        public static async Task QotdAsync(CommandContext context,
            [Description("The question to be added.")] string question)
            => await SuggestAsync(context, question);
    }
}
