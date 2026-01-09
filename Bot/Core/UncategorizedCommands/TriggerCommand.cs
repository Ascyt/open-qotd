using DSharpPlus.Commands;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.UncategorizedCommands
{
    public class TriggerCommand
    {
        [Command("trigger")]
        [Description("Trigger a QOTD prematurely.")]
        public static async Task TriggerAsync(CommandContext context)
        {
            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null || !await Permissions.Api.Admin.CheckAsync(context, config))
                return;

            await context.DeferResponseAsync();

            await QotdSending.Sender.Api.SendNextQotdAsync(context.Guild!, config, Notices.Api.GetLatestAvailableNotice());

            await context.RespondAsync(
                GenericEmbeds.Success(title:$"Successfully triggered {config.QotdShorthandText}", 
                message:$"A new {config.QotdTitleText} has been successfully sent to the <#{config.QotdChannelId}> channel."));

            await Logging.Api.LogUserActionAsync(context, config, "Trigger QOTD");
        }
    }
}
