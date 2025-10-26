using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Database;
using OpenQotd.Database.Entities;

namespace OpenQotd.Helpers.Profiles
{
    public class SuggestableProfilesAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> switchableFilteredProfiles = await GetSuggestableProfilesAsync(context, context.UserInput);

            return switchableFilteredProfiles
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }

        public static async Task<Dictionary<int, string>> GetSuggestableProfilesAsync(AbstractContext context, string? filter)
        {
            bool hasAdmin = context.Member!.Permissions.HasPermission(DiscordPermission.Administrator);
            ulong guildId = context.Guild!.Id;

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
                HashSet<ulong> userRoles = [.. context.Member!.Roles.Select(r => r.Id)];
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
