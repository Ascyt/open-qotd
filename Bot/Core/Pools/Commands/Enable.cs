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
    public sealed partial class PoolsCommand
    {
        [Command("enable")]
        [Description("Enable a pool.")]
        public static async Task EnablePoolAsync(CommandContext context, 
            [Description("The pool to enable.")][SlashAutoCompleteProvider<AutoCompleteProviders.ModifiablePools>] int pool)
        {
            int poolId = pool;

            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            Pool? foundPool;
            using (AppDbContext dbContext = new())
            {
                foundPool = await dbContext.Pools
                    .Where(p => p.Id == poolId && p.ConfigId == config.Id)
                    .FirstOrDefaultAsync();

                if (foundPool is null)
                {
                    await context.RespondAsync(GenericEmbeds.Error($"The specified pool with ID `{poolId}` does not exist."));
                    return;
                }

                if (!await Permissions.Api.Role.CheckAsync(foundPool.ModRoleId, context, config))
                    return;

                foundPool.Enabled = true;
                dbContext.Pools.Update(foundPool);
            }

            await context.RespondAsync(GenericEmbeds.Success(title: "Pool enabled", message: $"The pool \"{foundPool.Name}\" has been enabled successfully."));
            await Logging.Api.LogUserActionAsync(context, config, title:"Pool enabled", message:$"Pool \"{foundPool.Name}\" enabled.");
        }
    }
}
