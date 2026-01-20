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
        [Command("new")]
        [Description("Create a new pool.")]
        public static async Task NewPoolAsync(CommandContext context, 
            [Description("The name of the new pool.")] string name,
            [Description("Whether the new pool is enabled or disabled.")] bool enabled = true)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            if (string.IsNullOrWhiteSpace(name))
            {
                await context.RespondAsync("The pool name cannot be empty.");
                return;
            }
            if (name.Length > 100)
            {
                await context.RespondAsync("The pool name cannot be longer than 100 characters.");
                return;
            }

            using AppDbContext dbContext = new();

            int existingPoolsCount = await dbContext.Pools
                .Where(p => p.ConfigId == config.Id)
                .CountAsync();
            
            if (existingPoolsCount >= Program.AppSettings.PoolsPerGuildMaxAmount)
            {
                await context.RespondAsync($"Unable to create a new pool because the maximum amount of pools per guild ({Program.AppSettings.PoolsPerGuildMaxAmount}) has been reached.");
                return;
            }

            Pool newPool = new()
            {
                ConfigId = config.Id,
                Name = name,
                Enabled = enabled
            };
            dbContext.Pools.Add(newPool);
            await dbContext.SaveChangesAsync();

            await context.RespondAsync($"New pool '{name}' created successfully.");
        }
    }
}
