using CustomQotd.Features.Helpers;
using CustomQotd.Features.QotdSending;
using DSharpPlus.Commands;
using System.ComponentModel;

namespace CustomQotd.Features.Commands
{
    public class TriggerCommand
    {
        [Command("trigger")]
        [Description("Trigger a QOTD prematurely.")]
        public static async Task TriggerAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserIsAdmin(context))
                return;

            await QotdSender.SendNextQotd(context.Guild!.Id, Notices.GetLatestAvailableNotice());

            await context.RespondAsync(
                MessageHelpers.GenericSuccessEmbed(title:"Successfully triggered QOTD", "QOTD sent to current QOTD channel."));

            await Logging.LogUserAction(context, "Trigger QOTD");
        }
    }
}
