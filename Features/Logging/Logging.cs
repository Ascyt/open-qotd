using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace CustomQotd.Features.Logging
{
    public static class Logging
    {
        public static async Task LogUserAction(CommandContext context, string title, string? message = null) 
        {
            object? logChannelIdObject = await DatabaseApi.GetConfigValueAsync(context.Guild.Id, DatabaseValues.ConfigType.LogsChannelId);

            if (logChannelIdObject == null)
                return;

            string logChannelIdString = logChannelIdObject.ToString() ?? "";
            ulong logChannelId;
            DiscordChannel logChannel;

            try
            {
                if (!ulong.TryParse(logChannelIdString, out logChannelId))
                {
                    await PrintNotFoundWarning(context);
                    return;
                }

                logChannel = await context.Guild.GetChannelAsync(ulong.Parse(logChannelIdString));
            }
            catch (NotFoundException)
            {
                await PrintNotFoundWarning(context);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(new DiscordColor("#20ffff"))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"User `{context.User.Id}`")
                    .WithAuthor(name: context.User.Username, iconUrl: context.User.AvatarUrl);

            if (message != null) {
                embed.WithDescription(message);
            }

            await logChannel.SendMessageAsync(embed.Build());
        }

        private static async Task PrintNotFoundWarning(CommandContext context)
        {
            await context.Channel.SendMessageAsync(
                MessageHelpers.GenericWarningEmbed("Log channel is set, but not found.\n\n" +
                "*Set it using `/config set log_channel [channel]`, or unset it using `/config reset log_channel`.*")
                );
        }
    }
}
