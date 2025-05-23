﻿using CustomQotd.Database;
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
            await LogUserAction(context.Guild!.Id, context.Channel, context.User, title, message);
        }
        public static async Task LogUserAction(ulong guildId, DiscordChannel channel, DiscordUser user, string title, string? message = null)
        {
            ulong? logChannelId;
            using (var dbContext = new AppDbContext())
            {
                logChannelId = await dbContext.Configs.Where(c => c.GuildId == guildId).Select(c => c.LogsChannelId).FirstOrDefaultAsync();
            }

            if (logChannelId == null)
                return;

            DiscordChannel? logChannel = await GeneralHelpers.GetDiscordChannel(logChannelId.Value, guild:channel.Guild);
            if (logChannel is null)
            {
                await PrintNotFoundWarning(channel);
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithColor(new DiscordColor("#20ffff"))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"User ID: {user.Id}")
                    .WithAuthor(name: user.Username, iconUrl: user.AvatarUrl);

            if (message != null)
            {
                embed.WithDescription(message);
            }

            await logChannel.SendMessageAsync(embed.Build());
        }

        private static async Task PrintNotFoundWarning(DiscordChannel channel)
        {
            await channel.SendMessageAsync(
                MessageHelpers.GenericWarningEmbed("Log channel is set, but not found.\n\n" +
                "*It can be set using `/config set log_channel [channel]`, or unset using `/config reset log_channel`.*")
                );
        }
    }
}
