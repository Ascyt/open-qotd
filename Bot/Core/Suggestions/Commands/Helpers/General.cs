using DSharpPlus.Commands;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Suggestions.Commands.Helpers
{
    internal static class General
    {
        /// <summary>
        /// Check if the user has suggestions mod or admin permission, or, if there is no suggestions mod_role, if the command was run in the suggestions channel.
        /// </summary>
        public static async Task<bool> HasModOrIsInSuggestionsChannel(CommandContext context, Config config)
        {
            if (config.SuggestionsModRoleId is null) 
            {
                bool isInSuggestionsChannel = config.SuggestionsChannelId is not null && config.SuggestionsChannelId.Value == context.Channel.Id;
                if (!isInSuggestionsChannel)
                {
                    await context.RespondAsync(
                        GenericEmbeds.Error(title: "Incorrect Channel", message: $"This command can only be run in the <#{config.SuggestionsChannelId!.Value}> channel."));
                    return false;
                }
            }
            else 
            {
                if (!await Permissions.Api.SuggestionsMod.CheckAsync(context, config))
                    return false;
            }
            return true;
        }
    }
}
