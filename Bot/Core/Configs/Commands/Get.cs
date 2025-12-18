using DSharpPlus.Commands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Configs.Commands
{
    public sealed partial class ConfigCommand
    {
        [Command("get")]
        [Description("Get all config values")]
        public static async Task GetAsync(CommandContext context)
        {
            if (!await Permissions.Api.Admin.UserHasAdministratorPermission(context))
                return;

            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Info(title: $"Config values", message: $"{configString}"));
            Helpers.General.AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);
        }
    }
}
