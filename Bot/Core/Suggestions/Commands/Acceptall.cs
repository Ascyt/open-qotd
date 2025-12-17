using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Questions.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.Text;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using DSharpPlus.Exceptions;

namespace OpenQotd.Core.Suggestions.Commands
{
    public sealed partial class SuggestionsCommands
    {
        [Command("accepsall")]
        [Description("Accept all suggestions.")]
        public static async Task AcceptAllSuggestionsAsync(CommandContext context)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await Helpers.General.IsInSuggestionsChannelOrHasAdmin(context, config))
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
                await Suggestions.Helpers.AcceptDeny.AcceptSuggestionAsync(question, config, null, context, logAndNotify:false);

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
                                $"\"{General.Italicize(pair.Value[0].Text!)}\"\n\n" +
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

                            sb.AppendLine($"> - \"*{General.TrimIfNecessary(question.Text!, 64)}*\"");
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

            await Logging.Api.LogUserAction(context, config, $"Accepted all {count} Suggestions", logSb.ToString());

            await context.FollowupAsync(GenericEmbeds.Success("Suggestions Accepted", $"Successfully accepted {count} suggestion{(count == 1 ? "" : "s")}."));
        }
    }
}
