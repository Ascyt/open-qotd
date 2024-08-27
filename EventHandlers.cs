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
            string message = $"**{e.Exception.GetType().Name}**\n> {e.Exception.Message}\n\nStack Trace:\n```\n{e.Exception.StackTrace}";

            if (message.Length > 4096 - 5)
                message = message.Substring(0, 4096 - 5) + "…";

            await e.Context.RespondAsync(MessageHelpers.GenericErrorEmbed(message + "\n```", title: "Error (D#+)"));
        }
    }
}
