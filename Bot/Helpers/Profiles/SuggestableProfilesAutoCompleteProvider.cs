using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace OpenQotd.Bot.Helpers.Profiles
{
    public class SuggestableProfilesAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            Dictionary<int, string> switchableFilteredProfiles = await ProfileHelpers.GetSuggestableProfilesAsync(context, context.UserInput);

            return switchableFilteredProfiles
                .Take(25) // Max 25 choices allowed by Discord API
                .Select(kv => new DiscordAutoCompleteChoice(kv.Value, kv.Key));
        }
    }
}
