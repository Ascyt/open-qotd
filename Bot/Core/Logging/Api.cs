using DSharpPlus.Commands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Logging
{
    public static class Api
    {
        /// <summary>
        /// Logs a user action to the configured log channel, if set.
        /// </summary>
        public static async Task LogUserActionAsync(CommandContext context, Config config, string title, string? message = null)
        {
            await LogUserActionAsync(context.Channel, context.User, config, title, message);
        }

        /// <summary>
        /// Logs a user action to the configured log channel, if set.
        /// </summary>
        public static async Task LogUserActionAsync(DiscordChannel channel, DiscordUser user, Config config, string title, string? message = null)
        {
            if (!config.LogsChannelId.HasValue)
                return;

            DiscordChannel? logChannel = await Helpers.General.GetDiscordChannelAsync(config.LogsChannelId.Value, guild:channel.Guild);
            if (logChannel is null)
            {
                await PrintNotFoundWarningAsync(channel);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(new DiscordColor("#20ffff"))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"User ID: {user.Id} \x2022 Profile: {config.ProfileName}")
                    .WithAuthor(name: user.Username, iconUrl: user.AvatarUrl);

            if (message is not null)
            {
                embed.WithDescription(message);
            }

            await logChannel.SendMessageAsync(embed.Build());
        }

        private static async Task PrintNotFoundWarningAsync(DiscordChannel channel)
        {
            await channel.SendMessageAsync(
                GenericEmbeds.Warning("Log channel is set, but not found.\n\n" +
                "*It can be set using `/config set log_channel [channel]`, or unset using `/config reset log_channel`.*")
                );
        }
    }
}
