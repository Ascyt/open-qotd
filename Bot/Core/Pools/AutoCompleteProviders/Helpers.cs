using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Pools.Entities;

namespace OpenQotd.Core.Pools.AutoCompleteProviders
{
    public class Helpers 
    {
        public static async Task<Dictionary<int, string>> GetPoolsAsync(DiscordGuild guild, DiscordMember member, string? filter, bool requireAdmin)
        {
            Config? config = (await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(guild.Id, member.Id)).Item1;
            if (config is null)
                return [];

            bool isAdmin = (await Permissions.Api.Admin.CheckAsync(guild, member, config)).Item1;
            if (requireAdmin && !isAdmin)
                return [];

            ulong[] memberRolesAsAdminFallback = isAdmin ? [] : [.. member.Roles.Select(r => r.Id)];

            Pool[] pools; 
            using (AppDbContext dbContext = new())
            {
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                pools = await dbContext.Pools
                        .Where(p => p.ConfigId == config.Id && 
                            (!hasFilter || EF.Functions.ILike(p.Name, $"%{filter}%")) && 
                            (isAdmin || p.ModRoleId == null || memberRolesAsAdminFallback.Contains(p.ModRoleId.Value)) /* Only show pools that the user has access to */)
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
