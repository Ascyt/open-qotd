using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Permissions.Api
{
    public static class Basic
    {
        /// <summary>
        /// Checks if a user has basic or admin permission.
        /// </summary>
        /// <remarks>
        /// This also handles sending error messages, so it's recommended to end your function if it retuns false.
        /// </remarks>
        public static async Task<bool> CheckAsync(CommandContext context, Config? config, bool responseOnError = true)
            => await Role.CheckAsync(config?.BasicRoleId, context, config, responseOnError);

        /// <summary>
        /// See <see cref="CheckAsync(CommandContext, Config?, bool)"/>.
        /// </summary>
        /// <param name="member">Optionally provided to avoid re-fetching</param>
        public static async Task<bool> CheckAsync(InteractionCreatedEventArgs args, Config? config, DiscordMember? member = null, bool responseOnError=true)
            => await Role.CheckAsync(config?.BasicRoleId, args, config, member, responseOnError);
        /// <summary>
        /// See <see cref="CheckAsync(CommandContext, Config?, bool)"/>.
        /// </summary>
        public static async Task<(bool, string?)> CheckAsync(DiscordGuild guild, DiscordMember member, Config? config)
            => await Role.CheckAsync(config?.BasicRoleId, guild, member, config);
    }
}
