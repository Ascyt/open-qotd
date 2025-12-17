using System.ComponentModel;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Questions.Entities;

namespace OpenQotd.Core.Questions.Commands
{
    public sealed partial class QuestionsCommand
    {
        [Command("list")]
        [Description("List all questions.")]
        public static async Task ListQuestionsAsync(CommandContext context,
            [Description("The type of questions to show.")] QuestionType? type = null,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.UserIsAdmin(context, config))
                return;

            await ListQuestionsNoPermcheckAsync(context, config, type, page);
        }
        public static async Task ListQuestionsNoPermcheckAsync(CommandContext context, Config config, QuestionType? type = null, int page = 1)
        {
            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;

            await ListMessages.SendNew(context, page, type is null ? $"{config.QotdShorthandText} Questions List" : $"{type} {config.QotdShorthandText} Questions List", 
                async Task<PageInfo<Question>> (int page) =>
                {
                    using AppDbContext dbContext = new();

                    IQueryable<Question> sqlQuery;
                    if (type is null)
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.ConfigId == config.Id)
                            .OrderBy(q => q.Type)
                            .ThenByDescending(q => q.Timestamp)
                            .ThenByDescending(q => q.Id);
                    }
                    else
                    {
                        sqlQuery = dbContext.Questions
                            .Where(q => q.ConfigId == config.Id && q.Type == type)
                            .OrderByDescending(q => q.Timestamp)
                            .ThenByDescending(q => q.Id);
                    }

                    int totalElements = await sqlQuery
                        .CountAsync();

                    Question[] currentPageQuestions = await sqlQuery
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync();

                    return new PageInfo<Question>()
                    {
                        Elements = currentPageQuestions,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalElements
                    };
                });
        }
    }
}
