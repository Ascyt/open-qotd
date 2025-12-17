using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.EventHandlers.Helpers
{
    internal static class General
    {
        public static async Task<bool> HasExactlyNArguments(InteractionCreatedEventArgs args, string[] idArgs, int n)
        {
            if (idArgs.Length - 1 == n)
                return true;

            await RespondWithError(args, $"Component ID for `{idArgs[0]}` must have exactly {n} arguments (provided is {idArgs.Length - 1}).");
            return false;
        }

        public static async Task RespondWithError(InteractionCreatedEventArgs args, string message, string? title=null)
        {
            DiscordEmbed errorEmbed = GenericEmbeds.Error(title: title ?? "Error", message: message);

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(errorEmbed)
                .AsEphemeral());
        }
    }
}
