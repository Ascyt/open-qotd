using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Pools.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Pools.Commands
{
    public sealed partial class Pools
    {
        [Command("list")]
        [Description("Lists all Pools in the selected profile.")]
        public static async Task ListPoolsAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            await Helpers.ListMessages.SendNewAsync(context, page,
                title: $"Pools for {config.QotdShorthandText}",
                async Task<Helpers.PageInfo<Pool>> (int page) =>
                {
                    using AppDbContext dbContext = new();
                    int totalCount = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id)
                        .CountAsync();

                    int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;

                    Pool[] poolsInPage = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id)
                        .OrderBy(p => p.Id)
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)
                        .ToArrayAsync();

                    return new Helpers.PageInfo<Pool>
                    {
                        Elements = poolsInPage,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalCount
                    };
                });
        }
    }
}
