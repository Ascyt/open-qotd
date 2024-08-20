using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using System;

namespace CustomQotd
{
    public class EventHandlers
    {
        public static async Task CommandErrored(CommandsExtension s, CommandErroredEventArgs e)
        {
            await e.Context.RespondAsync(MessageHelpers.GenericErrorEmbed($"**{e.Exception.GetType().Name}**\n> {e.Exception.Message}\n\nStack Trace:\n```\n{e.Exception.StackTrace}```", title: "Error (D#+)"));
        }
    }
}
