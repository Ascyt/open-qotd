using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

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

            ulong guildId = context.Guild!.Id;
            ulong userId = context.User.Id;

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

            DiscordEmbed suggestResponseEmbed = MessageHelpers.GenericSuccessEmbed("QOTD Suggested!",
                    $"Your Question Of The Day:\n" +
                    $"> \"**{newQuestion.Text}**\"\n" +
                    $"\n" +
                    $"Has successfully been suggested! You'll be notified when it gets accepted or denied.");

            if (context is SlashCommandContext)
            {
                SlashCommandContext slashCommandcontext = context as SlashCommandContext;

                await slashCommandcontext.RespondAsync(suggestResponseEmbed, ephemeral:true);
            }
            else
            {
                await context.RespondAsync(suggestResponseEmbed);
            }

            await Logging.LogUserAction(context, "Suggested QOTD", $"> \"**{newQuestion.Text}**\"\nID: `{newQuestion.GuildDependentId}`");

            if (suggestionsChannelId == null)
            {
                if (suggestionsPingRoleId == null)
                {
                    await context.Channel.SendMessageAsync("Suggestions ping role is set, but suggestions channel is not.\n\n" +
                        "*The channel can be set using `/config set suggestions_channel [channel]`, or the ping role can be removed using `/config reset suggestions_ping_role`.*");
                }
                return;
            }

            DiscordRole? pingRole = null;
            if (suggestionsPingRoleId != null)
            {
                try
                {
                    pingRole = await context.Guild!.GetRoleAsync(suggestionsPingRoleId.Value);
                }
                catch (NotFoundException)
                {
                    await context.Channel.SendMessageAsync(
                        MessageHelpers.GenericWarningEmbed("Suggestions ping role is set, but not found.\n\n" +
                        "*It can be set using `/config set suggestions_ping_role [channel]`, or unset using `/config reset suggestions_ping_role`.*")
                        );
                }
            }

            DiscordChannel channel;
            try
            {
                channel = await context.Guild!.GetChannelAsync(suggestionsChannelId.Value);
            }
            catch (NotFoundException)
            {
                await context.Channel.SendMessageAsync(
                    MessageHelpers.GenericWarningEmbed("Suggestions channel is set, but not found.\n\n" +
                    "*It can be set using `/config set suggestions_channel [channel]`, or unset using `/config reset suggestions_channel`.*")
                    );
                return;
            }

            DiscordMessageBuilder messageBuilder = new();

            AddPingIfAvailable(messageBuilder, pingRole);

            string embedBody = $"> **{newQuestion.Text}**\n" +
                $"By: {context.Member!.Mention} (`{context.Member!.Id}`)\n" +
                $"ID: `{newQuestion.GuildDependentId}`";

            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed("A new QOTD Suggestion is available!", embedBody,
                color: "#d94f4f"));

            messageBuilder.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Success, "accept", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, "deny", "Deny")
            );

            DiscordMessage message = await channel.SendMessageAsync(
                    messageBuilder
                );

            using (var dbContext = new AppDbContext())
            {
                Question updateQuestion = await dbContext.Questions.FindAsync(newQuestion.Id);

                if (updateQuestion != null)
                {
                    updateQuestion.SuggestionMessageId = message.Id;

                    newQuestion = updateQuestion;
                }
            }

            var result = await message.WaitForButtonAsync();

            if (result.TimedOut)
            {
                DiscordMessageBuilder timeoutMessageBuilder = new();

                AddPingIfAvailable(timeoutMessageBuilder, pingRole);
                timeoutMessageBuilder.AddEmbed(MessageHelpers.GenericEmbed("A new QOTD Suggestion is available!", embedBody + 
                    $"\n\n**Use `/suggestions accept {newQuestion.GuildDependentId}` to accept or `/suggestions deny {newQuestion.GuildDependentId} [reason]` to deny this suggestion."));

                await message.ModifyAsync(
                    timeoutMessageBuilder
                    );
                return;
            }

            if (result.Result.Id == "accept") 
            {
                await SuggestionsCommands.AcceptSuggestionNoContextAsync(newQuestion, message);
                return;
            }

            if (result.Result.Id == "deny")
            {
                return;
            }
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
