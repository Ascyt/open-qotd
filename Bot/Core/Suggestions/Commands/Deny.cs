using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Questions.Entities;
using OpenQotd.Core.Database;
using DSharpPlus.Commands.Processors.SlashCommands;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Suggestions.Commands
{
    public sealed partial class SuggestionsCommand
    {
        [Command("deny")]
        [Description("Deny a suggestion.")]
        public static async Task DenySuggestionAsync(CommandContext context,
        [Description("The ID of the suggestion.")] int suggestionId)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            if (!await Helpers.General.IsInSuggestionsChannelOrHasAdmin(context, config))
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
                    GenericEmbeds.Error(title: "Mismatching Type", message: $"It's only possible to deny {Question.TypeToStyledString(QuestionType.Suggested)} questions, the provided question is of type {Question.TypeToStyledString(question.Type)}."));
                return;
            }

            await (context as SlashCommandContext)!.RespondWithModalAsync(Suggestions.Helpers.AcceptDeny.GetSuggestionDenyModal(config, question));
        }
    }
}
