using DSharpPlus.Commands;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Suggestions.Commands.Helpers
{
    internal static class General
    {
        /// <summary>
        /// Users in the suggestions channel can also accept/deny suggestions, even without admin permissions
        /// </summary>
        public static async Task<bool> IsInSuggestionsChannelOrHasAdmin(CommandContext context, Config config)
        {
            bool isInSuggestionsChannel = config.SuggestionsChannelId is not null && config.SuggestionsChannelId.Value == context.Channel.Id;
            if (!isInSuggestionsChannel && !await Permissions.Api.Admin.CheckAsync(context, config, responseOnError: config.SuggestionsChannelId is null))
            {
                if (config.SuggestionsChannelId is not null)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Incorrect Channel", message: $"This command can only be run in the <#{config.SuggestionsChannelId.Value}> channel."));
                }
                return false;
            }
            return true;
        }
    }
}
