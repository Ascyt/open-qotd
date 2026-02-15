using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Pools.Entities;

namespace OpenQotd.Core.Pools.AutoCompleteProviders
{
    public class ModifiablePools : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> filteredPools = await GetPoolsAsync(context.Guild!, context.Member!, context.UserInput);

            return filteredPools
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetPoolsAsync(DiscordGuild guild, DiscordMember member, string? filter)
            => await Helpers.GetPoolsAsync(guild, member, filter, requireAdmin: false);
    }
}
