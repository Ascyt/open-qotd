using ArtcordAdminBot.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using System;

namespace ArtcordAdminBot
{
    public class EventHandlers
    {
        public static async Task CommandErrored(CommandsExtension s, CommandErroredEventArgs e)
        {
            await e.Context.RespondAsync(MessageHelpers.GenericErrorEmbed($"**{e.Exception.GetType().Name}**\n> {e.Exception.Message}", title: "Error (D#+)"));
        }
    }
}
