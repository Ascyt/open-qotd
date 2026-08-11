using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;

namespace OpenQotd.Core.Profiles.AutoCompleteProviders
{
    public class SuggestableProfiles : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> suggestableFilteredProfiles = await GetSuggestableProfilesAsync(context.Guild!, context.Member!, context.UserInput);

            return suggestableFilteredProfiles
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetSuggestableProfilesAsync(DiscordGuild guild, DiscordMember member, string? filter)
        {
            bool hasAdmin = member.Permissions.HasPermission(DiscordPermission.Administrator);
            ulong guildId = guild.Id;

            Config[] configs;
            string defaultQotdTitle = Program.AppSettings.ConfigQotdTitleDefault;
            using (AppDbContext dbContext = new())
            {
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                configs = await dbContext.Configs
                        .Where(c => c.GuildId == guildId && c.EnableSuggestions && (!hasFilter || EF.Functions.ILike(c.QotdTitle ?? defaultQotdTitle, $"%{filter}%")))
                        .OrderByDescending(c => c.IsDefaultProfile) // Default profile first
                        .ThenByDescending(c => c.Id) // Then by ID (newer profiles first)
                        .ToArrayAsync();
            }

            Dictionary<int, string> suggestableProfiles = [];

            if (hasAdmin)
            {
                suggestableProfiles = configs
                    .ToDictionary(c => c.ProfileId, c => c.QotdTitle ?? defaultQotdTitle);
            }
            else
            {
                HashSet<ulong> userRoles = [.. member.Roles.Select(r => r.Id)];
                foreach (Config config in configs)
                {
                    bool hasBasicRole = config.BasicRoleId is null || userRoles.Contains(config.BasicRoleId.Value);
                    if (!hasBasicRole)
                        continue;

                    suggestableProfiles[config.ProfileId] = config.QotdTitle ?? defaultQotdTitle;
                }
            }

            return suggestableProfiles;
        }
    }
}
