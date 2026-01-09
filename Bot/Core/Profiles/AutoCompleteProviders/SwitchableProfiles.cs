using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;

namespace OpenQotd.Core.Profiles.AutoCompleteProviders
{
    public class SwitchableProfiles : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> switchableFilteredProfiles = await GetSwitchableProfilesAsync(context.Guild!, context.Member!, context.UserInput);

            return switchableFilteredProfiles
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetSwitchableProfilesAsync(DiscordGuild guild, DiscordMember member, string? filter)
        {
            bool hasAdmin = member.Permissions.HasPermission(DiscordPermission.Administrator);
            ulong guildId = guild.Id;

            Config[] configs;
            using (AppDbContext dbContext = new())
            {
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                configs = await dbContext.Configs
                        .Where(c => c.GuildId == guildId && (!hasFilter || EF.Functions.ILike(c.ProfileName, $"%{filter}%")))
                        .OrderByDescending(c => c.IsDefaultProfile) // Default profile first
                        .ThenByDescending(c => c.Id) // Then by ID (newer profiles first)
                        .ToArrayAsync();
            }
            int selectedProfileId = await Api.GetSelectedOrDefaultProfileIdAsync(guildId, member.Id);

            Dictionary<int, string> switchableProfiles = [];

            if (hasAdmin)
            {
                switchableProfiles = configs
                    .Where(c => c.ProfileId != selectedProfileId) // Exclude current profile
                    .ToDictionary(c => c.ProfileId, c => c.ProfileName);
            }
            else
            {
                HashSet<ulong> userRoles = [.. member.Roles.Select(r => r.Id)];
                foreach (Config config in configs)
                {
                    if (config.ProfileId == selectedProfileId)
                        continue; // Exclude current profile

                    bool hasAdminRole = userRoles.Contains(config.AdminRoleId);
                    if (!hasAdminRole)
                        continue;

                    switchableProfiles[config.ProfileId] = config.ProfileName;
                }
            }

            return switchableProfiles;
        }
    }
}
