using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenQotd.Bot.Commands
{
    [Command("suggestions")]
    public class SuggestionsCommands
    {
        // TODO: Make all of these available to all users if they are run in the set suggestions_channel.
        // Why? admin_role should only be used for things like questions or triggers, and suggestions should be a separate thing.
        // Currently, everyone can accept/deny suggestions with access to the suggestion channel using the buttons on the messages, and that's intentional.
        // However, the technical limitation makes it impossible (afaik) for the buttons to last indefinitely, because (again, afaik) it would require an endless loop, which I don't want to risk.
        // So the buttons shouldn't be necessary in some way, the /suggestions command with the ID should always be an alternative.
        // Also add this sort of explanation to documentation.

        private static async Task<DiscordMessage?> GetSuggestionMessage(Question question, DiscordGuild guild)
        {
            if (question.SuggestionMessageId is null)
                return null;

            ulong? suggestionChannelId;
            using (var dbContext = new AppDbContext())
            {
                suggestionChannelId = await dbContext.Configs.Where(c => c.GuildId == guild.Id).Select(c => c.SuggestionsChannelId).FirstOrDefaultAsync();
            }
            if (suggestionChannelId is null)
            {
                return null;
            }

            DiscordChannel? suggestionChannel = null;
            try
            {
                suggestionChannel = await guild.GetChannelAsync(suggestionChannelId.Value);
            }
            catch (NotFoundException)
            {
                return null;
            }

            if (suggestionChannel is null)
                return null;

            DiscordMessage? suggestionMessage = null;

            try
            {
                suggestionMessage = await suggestionChannel.GetMessageAsync(question.SuggestionMessageId.Value);
            }
            catch (NotFoundException)
            {
                return null;
            }

            return suggestionMessage;
        }

        public static string GetEmbedBody(Question question)
            => $"\"**{question.Text}**\"\n" +
                $"By: <@!{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)\n" +
                $"ID: `{question.GuildDependentId}`";

        [Command("accept")]
        [Description("Accept a suggestion.")]
        public static async Task AcceptSuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            Question? question;
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.GuildDependentId == suggestionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{suggestionId}` could not be found."));
                    return;
                }
            }

            if (question.Type != QuestionType.Suggested)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title: "Mismatching Type", message: $"It's only possible to accept **Suggested** questions, the provided question is of type **{question.Type}**."));
                return;
            }

            DiscordMessage? suggestionMessage = await GetSuggestionMessage(question, context.Guild!);

            await AcceptSuggestionNoContextAsync(question, suggestionMessage, null, context);

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Suggestion Accepted", $"Successfully accepted suggestion with ID `{question.GuildDependentId}`"));
        }

        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId,
        [Description("The reason why the suggestion is denied, which will be sent to the user.")] string reason)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            Question? question;
            using (var dbContext = new AppDbContext())
            {
                question = await dbContext.Questions
                    .Where(q => q.GuildId == context.Guild!.Id && q.GuildDependentId == suggestionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{suggestionId}` could not be found."));
                    return;
                }
            }

            if (question.Type != QuestionType.Suggested)
            {
                await context.RespondAsync(
                    MessageHelpers.GenericErrorEmbed(title: "Mismatching Type", message: $"It's only possible to deny **Suggested** questions, the provided question is of type **{question.Type}**."));
                return;
            }

            DiscordMessage? suggestionMessage = await GetSuggestionMessage(question, context.Guild!);

            string embedBody = GetEmbedBody(question);

            await DenySuggestionNoContextAsync(question, suggestionMessage, null, context, reason);

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Suggestion Denied", $"Successfully denied suggestion with ID `{question.GuildDependentId}`."));
        }

        [Command("acceptall")]
        [Description("Accept all suggestions.")]
        public static async Task AcceptAllSuggestionsAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            await context.DeferResponseAsync();

            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions.Where(q => q.GuildId == context.Guild!.Id && q.Type == QuestionType.Suggested).ToArrayAsync();
            }

            Dictionary<ulong, List<Question>> questionsByUsers = new();

            foreach (var question in questions)
            {
                DiscordMessage? suggestionMessage = await GetSuggestionMessage(question, context.Guild!);

                await AcceptSuggestionNoContextAsync(question, suggestionMessage, null, context, false);

                await Task.Delay(100); // Prevent rate-limit

                if (!questionsByUsers.ContainsKey(question.SubmittedByUserId))
                    questionsByUsers.Add(question.SubmittedByUserId, new List<Question>());

                questionsByUsers[question.SubmittedByUserId].Add(question);
            }

            foreach (KeyValuePair<ulong, List<Question>> pair in questionsByUsers)
            {
                DiscordUser? suggester;
                try
                {
                    suggester = await Program.Client.GetUserAsync(pair.Key);
                }
                catch (NotFoundException)
                {
                    suggester = null;
                }
                if (suggester is not null)
                {
                    DiscordMessageBuilder userSendMessage = new();

                    if (pair.Value.Count == 1)
                    {
                        userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{context.Guild!.Name}: QOTD Suggestion Accepted",
                                $"Your QOTD Suggestion:\n" +
                                $"\"**{pair.Value[0].Text}**\"\n\n" +
                                $"Has been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                                $"It is now qualified to appear as **Question Of The Day** in {context.Guild!.Name}!",
                                color: "#20ff20"
                            ).WithFooter($"Server ID: {context.Guild!.Id}"));
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder($"{pair.Value.Count} of your QOTD Suggestions:\n");

                        int index = 0;
                        foreach (Question question in pair.Value)
                        {
                            if (index >= 10)
                            {
                                sb.AppendLine($"> *+ {pair.Value.Count - 10} more*");
                                break;
                            }

                            sb.AppendLine($"> - \"**{question.Text}**\"");
                            index++;
                        }

                        sb.Append($"\nHave been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                            $"They are now qualified to appear as **Question Of The Day** in **{context.Guild!.Name}**!");

                        userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{context.Guild!.Name}: QOTD Suggestions Accepted", sb.ToString(), color: "#20ff20"));
                    }
                    await suggester.SendMessageAsync(
                        userSendMessage
                        );

                    await Task.Delay(1000); // Prevent rate-limit
                }
            }

            int count = questions.Count();

            StringBuilder logSb = new StringBuilder();
            int index1 = 0;
            foreach (Question question in questions)
            {
                if (index1 >= 10)
                {
                    logSb.AppendLine($"> *+ {count - 10} more*");
                    break;
                }

                logSb.AppendLine($"> - \"**{question.Text}**\" (ID: `{question.GuildDependentId}`)");
                index1++;
            }

            await Logging.LogUserAction(context, $"Accepted all {count} Suggestions", logSb.ToString());

            await context.FollowupAsync(MessageHelpers.GenericSuccessEmbed("Suggestions Accepted", $"Successfully accepted {count} suggestion{(count == 1 ? "" : "s")}."));
        }

        [Command("denyall")]
        [Description("Deny all suggestions.")]
        public static async Task DenyAllSuggestionsAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            await context.DeferResponseAsync();

            Question[] questions;
            using (var dbContext = new AppDbContext())
            {
                questions = await dbContext.Questions.Where(q => q.GuildId == context.Guild!.Id && q.Type == QuestionType.Suggested).ToArrayAsync();
            }

            Dictionary<ulong, List<Question>> questionsByUsers = new();

            foreach (var question in questions)
            {
                DiscordMessage? suggestionMessage = await GetSuggestionMessage(question, context.Guild!);

                await DenySuggestionNoContextAsync(question, suggestionMessage, null, context, null, false);

                await Task.Delay(1000); // Prevent rate-limit

                if (!questionsByUsers.ContainsKey(question.SubmittedByUserId))
                    questionsByUsers.Add(question.SubmittedByUserId, new List<Question>());

                questionsByUsers[question.SubmittedByUserId].Add(question);
            }

            foreach (KeyValuePair<ulong, List<Question>> pair in questionsByUsers)
            {
                DiscordUser? suggester;
                try
                {
                    suggester = await Program.Client.GetUserAsync(pair.Key);
                }
                catch (NotFoundException)
                {
                    suggester = null;
                }
                if (suggester is not null)
                {
                    DiscordMessageBuilder userSendMessage = new();

                    if (pair.Value.Count == 1)
                    {
                        userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{context.Guild!.Name}: QOTD Suggestion Denied",
                                $"Your QOTD Suggestion:\n" +
                                $"\"**{pair.Value[0].Text}**\"\n\n" +
                                $"Has been :x: **DENIED** :x:.",
                                color: "#ff2020"
                            ).WithFooter($"Server ID: {context.Guild!.Id}"));
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder($"{pair.Value.Count} of your QOTD Suggestions:\n");

                        int index = 0;
                        foreach (Question question in pair.Value)
                        {
                            if (index >= 10)
                            {
                                sb.AppendLine($"> *+ {pair.Value.Count - 10} more*");
                                break;
                            }

                            sb.AppendLine($"> - \"**{question.Text}**\"");
                            index++;
                        }

                        sb.Append($"\nHave been :x: **DENIED** :x:.");

                        userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{context.Guild!.Name}: QOTD Suggestions Denied", sb.ToString(), color: "#ff2020"));
                    }
                    await suggester.SendMessageAsync(
                        userSendMessage
                        );

                    await Task.Delay(100); // Prevent rate-limit
                }
            }

            int count = questions.Count();

            StringBuilder logSb = new StringBuilder();
            int index1 = 0;
            foreach (Question question in questions)
            {
                if (index1 >= 10)
                {
                    logSb.AppendLine($"> *+ {count - 10} more*");
                    break;
                }

                logSb.AppendLine($"> - \"**{question.Text}**\" (ID: `{question.GuildDependentId}`)");
                index1++;
            }

            await Logging.LogUserAction(context, $"Denied all {count} Suggestions", logSb.ToString());

            await context.FollowupAsync(MessageHelpers.GenericSuccessEmbed("Suggestions Denied", $"Successfully denied {count} suggestion{(count == 1 ? "" : "s")}."));
        }


        public static async Task AcceptSuggestionNoContextAsync(Question question, DiscordMessage? suggestionMessage, ComponentInteractionCreatedEventArgs? result, CommandContext? context, bool logAndNotify=true)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.User;
            DiscordGuild guild = context?.Guild ?? result!.Guild;

            using (var dbContext = new AppDbContext())
            {
                Question? modifyQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (modifyQuestion == null)
                {
                    DiscordEmbed embed = MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found.");

                    if (suggestionMessage is null)
                        await context!.Channel.SendMessageAsync(embed);
                    else
                        await suggestionMessage.Channel!.SendMessageAsync(embed
                        );
                    return;
                }

                modifyQuestion.Type = QuestionType.Accepted;
                modifyQuestion.AcceptedByUserId = user.Id;
                modifyQuestion.AcceptedTimestamp = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                question = modifyQuestion;
            }

            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                string embedBody = GetEmbedBody(question);

                messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Accepted", embedBody +
                    $"\n\nAccepted by: {user.Mention}", color: "#20ff20"));

                if (result is null)
                    await suggestionMessage.ModifyAsync(messageBuilder);
                else
                    await result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));

                await suggestionMessage.UnpinAsync();
            }

            if (!logAndNotify)
                return;

            DiscordUser? suggester;
            try
            {
                suggester = await Program.Client.GetUserAsync(question.SubmittedByUserId);
            }
            catch (NotFoundException)
            {
                suggester = null;
            }
            if (suggester is not null)
            {
                DiscordMessageBuilder userSendMessage = new();
                userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{guild.Name}: QOTD Suggestion Accepted",
                        $"Your QOTD Suggestion:\n" +
                        $"\"**{question.Text}**\"\n\n" +
                        $"Has been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                        $"It is now qualified to appear as **Question Of The Day** in **{guild.Name}**!",
                        color: "#20ff20"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context == null)
                await Logging.LogUserAction(suggestionMessage!.Channel!.Guild.Id, suggestionMessage.Channel, user, "Accepted Suggestion", question.ToString());
            else
                await Logging.LogUserAction(context, "Accepted Suggestion", question.ToString());
        }

        public static async Task DenySuggestionNoContextAsync(Question question, DiscordMessage? suggestionMessage, ModalSubmittedEventArgs? result, CommandContext? context, string? reason, bool logAndNotify = true)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.Interaction.User;
            DiscordGuild guild = context?.Guild ?? result!.Interaction.Guild!;

            using (var dbContext = new AppDbContext())
            {
                Question? disableQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (disableQuestion == null)
                {
                    DiscordEmbed embed = MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found.");

                    if (suggestionMessage is null)
                        await context!.Channel.SendMessageAsync(embed);
                    else 
                        await suggestionMessage.Channel!.SendMessageAsync(embed
                        );
                    return;
                }

                disableQuestion.Type = QuestionType.Stashed;

				await dbContext.SaveChangesAsync();
            }

            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.WithContent("");

                string embedBody = GetEmbedBody(question);

                messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Denied", embedBody +
                    $"\n\nDenied by: {user.Mention}{(reason != null ? $"\nReason: \"**{reason}**\"" : "")}", color: "#ff2020"));

                if (result is null)
                    await suggestionMessage.ModifyAsync(messageBuilder);
                else
                    await result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));

                await suggestionMessage.UnpinAsync();
            }

            if (!logAndNotify)
                return;

            DiscordUser? suggester;
            try
            {
                suggester = await Program.Client.GetUserAsync(question.SubmittedByUserId);
            }
            catch (NotFoundException)
            {
                suggester = null;
            }

            if (suggester is not null)
            {
                DiscordMessageBuilder userSendMessage = new();
                userSendMessage.AddEmbed(MessageHelpers.GenericEmbed($"{guild.Name}: QOTD Suggestion Denied",
                        $"Your QOTD Suggestion:\n" +
                        $"\"**{question.Text}**\"\n\n" +
                        $"Has been :x: **DENIED** :x: {(reason != null ? $"for the following reason:\n" +
                        $"> *{reason}*" : "")}",
                        color: "#ff2020"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context == null)
                await Logging.LogUserAction(suggestionMessage!.Channel!.Guild.Id, suggestionMessage.Channel, user, "Denied Suggestion", $"{question.ToString()}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
            else
                await Logging.LogUserAction(context, "Denied Suggestion", $"{question.ToString()}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
        }
    }
}
