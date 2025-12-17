using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Database;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using OpenQotd.QotdSending;
using System.ComponentModel;

namespace OpenQotd.Commands
{
    [Command("pools")]
    public class PoolsCommand
    {
        [Command("list")]
        [Description("Lists all Pools in the selected profile.")]
        public static async Task ListPoolsAsync(CommandContext context,
            [Description("Which OpenQOTD profile to consider.")][SlashAutoCompleteProvider<ViewableProfilesAutoCompleteProvider>] int of,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            int profileId = of;

            Config? config = await ProfileHelpers.TryGetConfigAsync(context, profileId);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            await ListMessages.SendNew(context, page,
                title: $"Pools for {config.QotdShorthandText}",
                async Task<PageInfo<Pool>> (int page) =>
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

                    return new PageInfo<Pool>
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
