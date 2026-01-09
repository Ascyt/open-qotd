using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {
        [Command("search")]
        [Description("Search all questions by a keyword.")]
        public static async Task SearchQuestionsAsync(CommandContext context,
            [Description("The search query (case-insensitive).")] string query,
            [Description("The type of questions to show (default all).")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNewAsync<Question>(context, page, $"{(type != null ? $"{type} " : "")}Questions Search for \"{query}\"", 
                async Task<PageInfo<Question>> (int page) =>
                {
                    using AppDbContext dbContext = new();

                    IQueryable<Question> sqlQuery = dbContext.Questions
                        .Where(q => q.ConfigId == config.Id && (type == null || q.Type == type))
                        .Where(q => EF.Functions.Like(q.Text, $"%{query}%"));

                    int totalQuestions = await sqlQuery.CountAsync();

                    Question[] questions = await sqlQuery
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync();

                    return new PageInfo<Question>()
                    {
                        Elements = questions,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalQuestions,
                    };
                });
        }
    }
}
