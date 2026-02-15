using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Pools.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Pools.Commands
{
    public sealed partial class PoolsCommand
    {
        [Command("list")]
        [Description("Lists all Pools in the selected profile.")]
        public static async Task ListPoolsAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1,
            [Description("Whether to only show enabled/disabled pools.")] bool? onlyEnabled = null)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            bool isAdmin = await Permissions.Api.Admin.CheckAsync(context, config);
            ulong[] memberRolesAsAdminFallback = isAdmin || context.Member is null ? [] : [.. context.Member.Roles.Select(r => r.Id)];

            await Helpers.ListMessages.SendNewAsync(context, page,
                title: $"{(isAdmin ? "All" : "Modifiable")} Pools for {config.QotdShorthandText}",
                async Task<Helpers.PageInfo<Pool>> (int page) =>
                {
                    using AppDbContext dbContext = new();
                    int totalCount = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id)
                        .CountAsync();

                    int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;

                    Pool[] poolsInPage = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id && 
                            (onlyEnabled == null || p.Enabled == onlyEnabled.Value) &&
                            (isAdmin || p.ModRoleId == null || memberRolesAsAdminFallback.Contains(p.ModRoleId.Value)))
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
