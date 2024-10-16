﻿using CommunityToolkit.HighPerformance.Helpers;
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

            if (!await Question.CheckTextValidity(question, context))
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
                    $"Has successfully been suggested!\n" +
                    $"You will be notified when it gets accepted or denied.");

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

            string embedBody = $"> \"**{newQuestion.Text}**\"\n" +
                $"By: {context.Member!.Mention} (`{context.Member!.Id}`)\n" +
                $"ID: `{newQuestion.GuildDependentId}`";

            messageBuilder.AddEmbed(MessageHelpers.GenericEmbed("A new QOTD Suggestion is available!", embedBody,
                color: "#f0b132"));

            messageBuilder.AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Success, "accept", "Accept"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, "deny", "Deny")
            );

            DiscordMessage message = await channel.SendMessageAsync(
                    messageBuilder
                );

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

            InteractivityResult<ComponentInteractionCreatedEventArgs>? resultNullable = null;
            // Generally timeouts after one minute, so a TTL of 15 means 15 minutes
            int timeToLive = 120;
            do
            {
                if (timeToLive <= 0)
                {
                    await OnTimeoutAsync();
                    break;
                }

                resultNullable = await message.WaitForButtonAsync();

                timeToLive--;
            }
            while (resultNullable.Value.TimedOut);

            var result = resultNullable!.Value;

            async Task<bool> QuestionAlteredOrDeleted()
            {
                Question? updatedQuestion;
                using (var dbContext = new AppDbContext())
                {
                    updatedQuestion = await dbContext.Questions.FindAsync(newQuestion.Id);
                }
                return updatedQuestion is null || updatedQuestion.Type != QuestionType.Suggested;
            }
            async Task OnTimeoutAsync()
            {
                if (await QuestionAlteredOrDeleted())
                    return;

                DiscordMessageBuilder timeoutMessageBuilder = new();

                AddPingIfAvailable(timeoutMessageBuilder, pingRole);
                timeoutMessageBuilder.AddEmbed(MessageHelpers.GenericEmbed("A new QOTD Suggestion is available!", embedBody +
                    $"\n\n*Use `/suggestions accept {newQuestion.GuildDependentId}` to accept,\nor `/suggestions deny {newQuestion.GuildDependentId} [reason]` to deny this suggestion.*", color: "#f0b132"));

                await message.ModifyAsync(
                    timeoutMessageBuilder
                    );
                return;
            }

            try
            {
                if (result.Result is null)
                {
                    await OnTimeoutAsync();
                    return;
                }

                if (result.Result.Id == "accept")
                {
                    await SuggestionsCommands.AcceptSuggestionNoContextAsync(newQuestion, message, result, null, embedBody);
                    return;
                }

                if (result.Result.Id == "deny")
                {
                    DiscordMessageBuilder editMessage = new();

                    editMessage.AddEmbed(
                        MessageHelpers.GenericEmbed("QOTD Suggestion is awaiting denial reason", embedBody +
                        $"\n\nAwaiting denial reason by: {result.Result.User.Mention}", color: "#ff2020")
                        );

                    await message.ModifyAsync(editMessage);

                    int denyTimeToLive = 10;

                    DiscordMessageBuilder denyMessageBuilder = new();
                    denyMessageBuilder.WithContent(result.Result.User.Mention);
                    denyMessageBuilder.WithAllowedMention(new UserMention(result.Result.User));
                    denyMessageBuilder.WithReply(message.Id);
                    denyMessageBuilder.AddEmbed(
                        MessageHelpers.GenericWarningEmbed(title: "Denial Reason Required", message:
                        "To continue, type the reason for the denial into this channel.\n" +
                        "This will be sent to the user whose suggestion is denied.\n\n" +
                        $"*The first message sent into this channel by user {result.Result.User.Mention} will be provided as reason.*\n" +
                        $"*Respond with \"qotd:cancel\" to cancel.*\n" +
                        $"*Expires {DSharpPlus.Formatter.Timestamp(DateTime.UtcNow.Add(TimeSpan.FromMinutes(denyTimeToLive)), DSharpPlus.TimestampFormat.RelativeTime)}.*"));

                    DiscordMessage denyMessage = await channel.SendMessageAsync(denyMessageBuilder);
                    bool timeout = false;
                    InteractivityResult<DiscordMessage>? reasonResultNullable = null;
                    do
                    {
                        if (denyTimeToLive <= 0)
                        {
                            timeout = true;
                            break;
                        }

                        reasonResultNullable = await denyMessage.Channel!.GetNextMessageAsync(m =>
                        {
                            return (m.Author!.Id == result.Result.User.Id || m.Content == "qotd:cancel");
                        });

                        denyTimeToLive--;
                    }
                    while (reasonResultNullable.Value.TimedOut);

                    if (timeout || reasonResultNullable.Value.Result == null)
                    {
                        DiscordMessageBuilder timeoutDenyMessageBuilder = new();

                        timeoutDenyMessageBuilder.WithContent(result.Result.User.Mention);
                        timeoutDenyMessageBuilder.WithAllowedMention(new UserMention(result.Result.User));
                        timeoutDenyMessageBuilder.WithReply(message.Id);
                        timeoutDenyMessageBuilder.AddEmbed(
                            MessageHelpers.GenericErrorEmbed(title: "Denial Reason Required: Error (expired)", message:
                            "This action has expired, and the suggestion has not been denied."));

                        await denyMessage.ModifyAsync(timeoutDenyMessageBuilder);

                        await OnTimeoutAsync();

                        return;
                    }

                    if (reasonResultNullable.Value.Result.Content == "qotd:cancel")
                    {
                        DiscordMessageBuilder timeoutDenyMessageBuilder = new();

                        timeoutDenyMessageBuilder.WithReply(message.Id);
                        timeoutDenyMessageBuilder.AddEmbed(
                            MessageHelpers.GenericErrorEmbed(title: "Denial Reason Required: Cancelled", message:
                            "This action has been cancelled, and the suggestion has not been denied."));

                        await denyMessage.ModifyAsync(timeoutDenyMessageBuilder);

                        await reasonResultNullable.Value.Result.DeleteAsync();

                        await OnTimeoutAsync();

                        return;
                    }

                    if (await QuestionAlteredOrDeleted())
                    {
                        DiscordMessageBuilder alteredDenyBuilder = new();

                        alteredDenyBuilder.WithContent(result.Result.User.Mention);
                        alteredDenyBuilder.WithAllowedMention(new UserMention(result.Result.User));
                        alteredDenyBuilder.WithReply(message.Id);
                        alteredDenyBuilder.AddEmbed(
                            MessageHelpers.GenericErrorEmbed(title: "Denial Reason Required: Error (modified)", message:
                            "The question could not be denied because it had been denied or accept from another source."));

                        await denyMessage.ModifyAsync(alteredDenyBuilder);

                        return;
                    }

                    await denyMessage.DeleteAsync();

                    string reason = reasonResultNullable.Value.Result.Content;

                    await reasonResultNullable.Value.Result.DeleteAsync();

                    await SuggestionsCommands.DenySuggestionNoContextAsync(newQuestion, message, result, null, embedBody, reason);

                    return;
                }
            }
            catch (System.NullReferenceException)
            {
                await OnTimeoutAsync();
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
