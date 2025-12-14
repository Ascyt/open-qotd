using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Text;
using OpenQotd.Helpers.Profiles;
using DSharpPlus.Commands.Processors.SlashCommands;
using OpenQotd.Helpers.Suggestions;

namespace OpenQotd.Commands
{
    [Command("suggestions")]
    public class SuggestionsCommands
    {
        /// <summary>
        /// Users in the suggestions channel can also accept/deny suggestions, even without admin permissions
        /// </summary>
        private static async Task<bool> IsInSuggestionsChannelOrHasAdmin(CommandContext context, Config config)
        {
            bool isInSuggestionsChannel = config.SuggestionsChannelId is not null && config.SuggestionsChannelId.Value == context.Channel.Id;
            if (!isInSuggestionsChannel && !await CommandRequirements.UserIsAdmin(context, config, responseOnError: config.SuggestionsChannelId is null))
            {
                if (config.SuggestionsChannelId is not null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Incorrect Channel", message: $"This command can only be run in the <#{config.SuggestionsChannelId.Value}> channel."));
                }
                return false;
            }
            return true;
        }

        [Command("accept")]
        [Description("Accept a suggestion.")]
        public static async Task AcceptSuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await IsInSuggestionsChannelOrHasAdmin(context, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == suggestionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{suggestionId}` could not be found in profile *{config.ProfileName}*."));
                    return;
                }
            }

            if (question.Type != QuestionType.Suggested)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Mismatching Type", message: $"It's only possible to accept **Suggested** questions, the provided question is of type **{question.Type}**."));
                return;
            }

            await SuggestionsAcceptDenyHelpers.AcceptSuggestionAsync(question, config, null, context);

            await context.RespondAsync(
                GenericEmbeds.Success("Suggestion Accepted", $"Successfully accepted suggestion with ID `{question.GuildDependentId}`"));
        }

        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await IsInSuggestionsChannelOrHasAdmin(context, config))
                return;

            Question? question;
            using (AppDbContext dbContext = new())
            {
                question = await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id && q.GuildDependentId == suggestionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Suggestion Not Found", message: $"The suggestion with ID `{suggestionId}` could not be found in profile *{config.ProfileName}*."));
                    return;
                }
            }

            if (question.Type != QuestionType.Suggested)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error(title: "Mismatching Type", message: $"It's only possible to deny **Suggested** questions, the provided question is of type **{question.Type}**."));
                return;
            }

            await (context as SlashCommandContext)!.RespondWithModalAsync(GeneralHelpers.GetSuggestionDenyModal(config, question));
        }

        [Command("acceptall")]
        [Description("Accept all suggestions.")]
        public static async Task AcceptAllSuggestionsAsync(CommandContext context)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await IsInSuggestionsChannelOrHasAdmin(context, config))
                return;

            await context.DeferResponseAsync();

            Question[] questions;
            using (AppDbContext dbContext = new())
            {
                questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Suggested).ToArrayAsync();
            }

            Dictionary<ulong, List<Question>> questionsByUsers = [];

            foreach (Question question in questions)
            {
                await SuggestionsAcceptDenyHelpers.AcceptSuggestionAsync(question, config, null, context, false);

                await Task.Delay(100); // Prevent rate-limit

                if (!questionsByUsers.ContainsKey(question.SubmittedByUserId))
                    questionsByUsers.Add(question.SubmittedByUserId, []);

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
                        userSendMessage.AddEmbed(GenericEmbeds.Custom($"{context.Guild!.Name}: {config.QotdShorthandText} Suggestion Accepted",
                                $"Your {config.QotdShorthandText} Suggestion:\n" +
                                $"\"{GeneralHelpers.Italicize(pair.Value[0].Text!)}\"\n\n" +
                                $"Has been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                                $"It is now qualified to appear as **{config.QotdTitleText}** in {context.Guild!.Name}!",
                                color: "#20ff20"
                            ).WithFooter($"Server ID: {context.Guild!.Id}"));
                    }
                    else
                    {
                        StringBuilder sb = new($"{pair.Value.Count} of your {config.QotdShorthandText} Suggestions:\n");

                        int index = 0;
                        foreach (Question question in pair.Value)
                        {
                            if (index >= 10)
                            {
                                sb.AppendLine($"> *+ {pair.Value.Count - 10} more*");
                                break;
                            }

                            sb.AppendLine($"> - \"*{GeneralHelpers.TrimIfNecessary(question.Text!, 64)}*\"");
                            index++;
                        }

                        sb.Append($"\nHave been :white_check_mark: **ACCEPTED** :white_check_mark:!\n" +
                            $"They are now qualified to appear as **{config.QotdTitleText}** in **{context.Guild!.Name}**!");

                        userSendMessage.AddEmbed(GenericEmbeds.Custom($"{context.Guild!.Name}: {config.QotdShorthandText} Suggestions Accepted", sb.ToString(), color: "#20ff20"));
                    }
                    await suggester.SendMessageAsync(
                        userSendMessage
                        );

                    await Task.Delay(1000); // Prevent rate-limit
                }
            }

            int count = questions.Length;

            StringBuilder logSb = new();
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

            await Logging.LogUserAction(context, config, $"Accepted all {count} Suggestions", logSb.ToString());

            await context.FollowupAsync(GenericEmbeds.Success("Suggestions Accepted", $"Successfully accepted {count} suggestion{(count == 1 ? "" : "s")}."));
        }

        [Command("denyall")]
        [Description("Deny all suggestions.")]
        public static async Task DenyAllSuggestionsAsync(CommandContext context)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await IsInSuggestionsChannelOrHasAdmin(context, config))
                return;

            await context.DeferResponseAsync();

            Question[] questions;
            using (AppDbContext dbContext = new())
            {
                questions = await dbContext.Questions.Where(q => q.ConfigId == config.Id && q.Type == QuestionType.Suggested).ToArrayAsync();
            }

            Dictionary<ulong, List<Question>> questionsByUsers = [];

            foreach (Question question in questions)
            {
                await SuggestionsAcceptDenyHelpers.DenySuggestionAsync(question, config, null, context, null, false);

                await Task.Delay(1000); // Prevent rate-limit

                if (!questionsByUsers.ContainsKey(question.SubmittedByUserId))
                    questionsByUsers.Add(question.SubmittedByUserId, []);

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
                        userSendMessage.AddEmbed(GenericEmbeds.Custom($"{context.Guild!.Name}: {config.QotdShorthandText} Suggestion Denied",
                                $"Your {config.QotdShorthandText} Suggestion:\n" +
                                $"\"{GeneralHelpers.Italicize(pair.Value[0].Text!)}\"\n\n" +
                                $"Has been :x: **DENIED** :x:.",
                                color: "#ff2020"
                            ).WithFooter($"Server ID: {context.Guild!.Id}"));
                    }
                    else
                    {
                        StringBuilder sb = new($"{pair.Value.Count} of your {config.QotdTitleText} Suggestions:\n");

                        int index = 0;
                        foreach (Question question in pair.Value)
                        {
                            if (index >= 10)
                            {
                                sb.AppendLine($"> *+ {pair.Value.Count - 10} more*");
                                break;
                            }

                            sb.AppendLine($"> - \"*{GeneralHelpers.TrimIfNecessary(question.Text!, 64)}*\"");
                            index++;
                        }

                        sb.Append($"\nHave been :x: **DENIED** :x:.");

                        userSendMessage.AddEmbed(GenericEmbeds.Custom($"{context.Guild!.Name}: {config.QotdShorthandText} Suggestions Denied", sb.ToString(), color: "#ff2020"));
                    }
                    await suggester.SendMessageAsync(
                        userSendMessage
                        );

                    await Task.Delay(100); // Prevent rate-limit
                }
            }

            int count = questions.Length;

            StringBuilder logSb = new();
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

            await Logging.LogUserAction(context, config, $"Denied all {count} Suggestions", logSb.ToString());

            await context.FollowupAsync(GenericEmbeds.Success("Suggestions Denied", $"Successfully denied {count} suggestion{(count == 1 ? "" : "s")}."));
        }
    }
}
