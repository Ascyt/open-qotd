using CustomQotd.Database;
using CustomQotd.Features.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Features
{
    public static class Logging
    {
        public static async Task LogUserAction(CommandContext context, string title, string? message = null)
        {
            ulong? logChannelId;
            using (var dbContext = new AppDbContext())
            {
                logChannelId = await dbContext.Configs.Where(c => c.GuildId == context.Guild.Id).Select(c => c.LogsChannelId).FirstOrDefaultAsync();
            }

            if (logChannelId == null)
                return;

            DiscordChannel? logChannel = await GeneralHelpers.GetDiscordChannel(logChannelId.Value, commandContext:context);
            if (logChannel is null)
            {
                await PrintNotFoundWarning(context);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(new DiscordColor("#20ffff"))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"User ID: {context.User.Id}")
                    .WithAuthor(name: context.User.Username, iconUrl: context.User.AvatarUrl);

            if (message != null)
            {
                embed.WithDescription(message);
            }

            await logChannel.SendMessageAsync(embed.Build());
        }

        private static async Task PrintNotFoundWarning(CommandContext context)
        {
            await context.Channel.SendMessageAsync(
                MessageHelpers.GenericWarningEmbed("Log channel is set, but not found.\n\n" +
                "*It can be set using `/config set log_channel [channel]`, or unset using `/config reset log_channel`.*")
                );
        }
    }
}
