using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.Core.Pools.Entities;
using System.ComponentModel;

namespace OpenQotd.Core.Pools.Commands
{
    public sealed partial class Pools
    {
        [Command("delete")]
        [Description("Delete an existing pool.")]
        public static async Task DeletePoolAsync(CommandContext context, 
            [Description("The pool to delete.")][SlashAutoCompleteProvider<AutoCompleteProviders.RequireAdminPools>] int pool)
        {
            int poolId = pool;

            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            Pool? poolToDelete;
            using (AppDbContext dbContext = new())
            {
                poolToDelete = await dbContext.Pools
                    .Where(p => p.Id == poolId && p.ConfigId == config.Id)
                    .FirstOrDefaultAsync();

                if (poolToDelete is null)
                {
                    await context.RespondAsync(GenericEmbeds.Error("The specified pool does not exist."));
                    return;
                }

                dbContext.Pools.Remove(poolToDelete);
                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync($"The pool \"{poolToDelete.Name}\" has been deleted successfully.");
        }
    }
}
