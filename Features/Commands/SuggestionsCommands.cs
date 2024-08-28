using CustomQotd.Database;
using CustomQotd.Database.Entities;
using CustomQotd.Features.Helpers;
using CustomQotd.Migrations;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Threading.Channels;

namespace CustomQotd.Features.Commands
{
    [Command("suggestions")]
    public class SuggestionsCommands
    {
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

        [Command("accept")]
        [Description("Accept a suggestion.")]
        public static async Task AcceptSuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {
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

            string embedBody = $"> \"**{question.Text}**\"\n" +
                $"By: <@!{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)\n" +
                $"ID: `{question.GuildDependentId}`";

            await AcceptSuggestionNoContextAsync(question, suggestionMessage, null, context, embedBody:embedBody);

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Suggestion Accepted", $"Successfully accepted suggestion with ID `{question.GuildDependentId}`"));
        }

        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId,
        [Description("The reason why the suggestion is denied, which will be sent to the user.")] string reason)
        {
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

            string embedBody = $"> \"**{question.Text}**\"\n" +
                $"By: <@!{question.SubmittedByUserId}> (`{question.SubmittedByUserId}`)\n" +
                $"ID: `{question.GuildDependentId}`";

            await DenySuggestionNoContextAsync(question, suggestionMessage, null, context, embedBody: embedBody, reason);

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed("Suggestion Denied", $"Successfully denied suggestion with ID `{question.GuildDependentId}`."));
        }

        public static async Task AcceptSuggestionNoContextAsync(Question question, DiscordMessage? suggestionMessage, InteractivityResult<ComponentInteractionCreatedEventArgs>? result, CommandContext? context, string embedBody)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.Value.Result.User;
            DiscordGuild guild = context?.Guild ?? result!.Value.Result.Guild;

            using (var dbContext = new AppDbContext())
            {
                Question? modifyQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (modifyQuestion == null)
                {
                    DiscordEmbed embed = MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found.");

                    if (suggestionMessage is null)
                        await context.Channel.SendMessageAsync(embed);
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

                messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Accepted", embedBody +
                    $"\n\nAccepted by: {user.Mention}", color: "#20ff20"));

                await suggestionMessage.ModifyAsync(messageBuilder);
            }

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
                        $"> \"**{question.Text}**\"\n\n" +
                        $"Has been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                        $"It is now qualified to appear as **Question Of The Day** in {guild.Name}!",
                        color: "#20ff20"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context == null)
                await Logging.LogUserAction(suggestionMessage.Channel!.Guild.Id, suggestionMessage.Channel, user, "Accepted Suggestion", question.ToString());
            else
                await Logging.LogUserAction(context, "Accepted Suggestion", question.ToString());
        }

        public static async Task DenySuggestionNoContextAsync(Question question, DiscordMessage? suggestionMessage, InteractivityResult<ComponentInteractionCreatedEventArgs>? result, CommandContext? context, string embedBody, string reason)
        {
            if (result == null && context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DiscordUser user = context?.User ?? result!.Value.Result.User;
            DiscordGuild guild = context?.Guild ?? result!.Value.Result.Guild;

            using (var dbContext = new AppDbContext())
            {
                Question? removeQuestion = await dbContext.Questions.FindAsync(question.Id);

                if (removeQuestion == null)
                {
                    DiscordEmbed embed = MessageHelpers.GenericErrorEmbed(title: "Suggestion Not Found", message: $"The suggestion with ID `{question.GuildDependentId}` could not be found.");

                    if (suggestionMessage is null)
                        await context.Channel.SendMessageAsync(embed);
                    else
                        await suggestionMessage.Channel!.SendMessageAsync(embed
                        );
                    return;
                }

                dbContext.Questions.Remove(removeQuestion); 

                await dbContext.SaveChangesAsync();
            }

            if (suggestionMessage is not null)
            {
                DiscordMessageBuilder messageBuilder = new();

                messageBuilder.AddEmbed(MessageHelpers.GenericEmbed($"QOTD Suggestion Denied", embedBody +
                    $"\n\nDenied by: {user.Mention}\nReason: \"**{reason}**\"", color: "#ff2020"));

                await suggestionMessage.ModifyAsync(messageBuilder);
            }

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
                        $"> \"**{question.Text}**\"\n\n" +
                        $"Has been :x: **DENIED** :x: for the following reason:\n" +
                        $"> *{reason}*",
                        color: "#ff2020"
                    ).WithFooter($"Server ID: {guild.Id}"));

                await suggester.SendMessageAsync(
                    userSendMessage
                    );
            }

            if (context == null)
                await Logging.LogUserAction(suggestionMessage.Channel!.Guild.Id, suggestionMessage.Channel, user, "Denied Suggestion", $"{question.ToString()}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
            else
                await Logging.LogUserAction(context, "Denied Suggestion", $"{question.ToString()}\n\n" +
                $"Denial Reason: \"**{reason}**\"");
        }

        // TODO: acceptall, denyall
    }
}
