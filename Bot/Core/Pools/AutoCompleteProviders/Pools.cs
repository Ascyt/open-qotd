using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Pools.Entities;

namespace OpenQotd.Core.Pools.AutoCompleteProviders
{
    public class Pools : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> filteredPools = await GetPoolsAsync(context.Guild!, context.Member!, context.UserInput);

            return filteredPools
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetPoolsAsync(DiscordGuild guild, DiscordMember member, string? filter)
        {
            Config? config = (await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(guild.Id, member.Id)).Item1;
            if (config is null || !(await Permissions.Api.Admin.CheckAsync(guild, member, config)).Item1)
                return [];

            Pool[] pools; 
            using (AppDbContext dbContext = new())
            {
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                pools = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id && (!hasFilter || EF.Functions.ILike(p.Name, $"%{filter}%")))
                        .OrderByDescending(p => p.Enabled) // Enabled pools first
                        .ThenBy(p => p.Name) // Then alphabetically by name
                        .ToArrayAsync();
            }

            Dictionary<int, string> viewablePools = pools
                .ToDictionary(p => p.Id, p => p.Name);

            return viewablePools;
        }
    }
}
