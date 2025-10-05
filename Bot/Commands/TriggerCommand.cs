using DSharpPlus.Commands;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;
using OpenQotd.Bot.QotdSending;
using System.ComponentModel;

namespace OpenQotd.Bot.Commands
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
