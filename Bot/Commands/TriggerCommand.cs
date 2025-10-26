using DSharpPlus.Commands;
using OpenQotd.Database.Entities;
using OpenQotd.Helpers;
using OpenQotd.Helpers.Profiles;
using OpenQotd.QotdSending;
using System.ComponentModel;

namespace OpenQotd.Commands
{
    public class TriggerCommand
    {
        [Command("trigger")]
        [Description("Trigger a QOTD prematurely.")]
        public static async Task TriggerAsync(CommandContext context)
        {
            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await CommandRequirements.UserIsAdmin(context, config))
                return;

            await context.DeferResponseAsync();

            await QotdSender.SendNextQotdAsync(context.Guild!, config, Notices.GetLatestAvailableNotice());

            await context.RespondAsync(
                GenericEmbeds.Success(title:$"Successfully triggered {config.QotdShorthandText}", 
                message:$"A new {config.QotdTitleText} has been successfully sent to the <#{config.QotdChannelId}> channel."));

            await Logging.LogUserAction(context, config, "Trigger QOTD");
        }
    }
}
