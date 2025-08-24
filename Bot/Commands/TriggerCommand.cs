using CustomQotd.Bot.QotdSending;
using CustomQotd.Bot.Helpers;
using DSharpPlus.Commands;
using System.ComponentModel;

namespace CustomQotd.Bot.Commands
{
    public class TriggerCommand
    {
        [Command("trigger")]
        [Description("Trigger a QOTD prematurely.")]
        public static async Task TriggerAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserIsAdmin(context, null))
                return;

            await context.DeferResponseAsync();

            await QotdSender.SendNextQotdAsync(context.Guild!, Notices.GetLatestAvailableNotice());

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed(title:"Successfully triggered QOTD", "QOTD sent to current QOTD channel."));

            await Logging.LogUserAction(context, "Trigger QOTD");
        }
    }
}
