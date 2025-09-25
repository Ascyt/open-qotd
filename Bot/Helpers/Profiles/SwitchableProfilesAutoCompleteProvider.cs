using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.Helpers.Profiles
{
    public class SwitchableProfilesAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> switchableFilteredProfiles = await GetSwitchableProfilesAsync(context, context.UserInput);

            return switchableFilteredProfiles
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetSwitchableProfilesAsync(AbstractContext context, string? filter)
        {
            bool hasAdmin = context.Member!.Permissions.HasPermission(DiscordPermission.Administrator);
            ulong guildId = context.Guild!.Id;

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
            int selectedProfileId = await ProfileHelpers.GetSelectedOrDefaultProfileIdAsync(guildId, context.User.Id);

            Dictionary<int, string> switchableProfiles = [];

            if (hasAdmin)
            {
                switchableProfiles = configs
                    .Where(c => c.ProfileId != selectedProfileId) // Exclude current profile
                    .ToDictionary(c => c.ProfileId, c => c.ProfileName);
            }
            else
            {
                HashSet<ulong> userRoles = [.. context.Member!.Roles.Select(r => r.Id)];
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
