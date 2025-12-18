using DSharpPlus.Commands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Configs.Commands
{
    public sealed partial class ConfigCommand
    {        
        public enum SingleOption
        {
            Reset
        }

        [Command("reset")]
        public sealed class ConfigResetCommand
        {
            [Command("general")]
            [Description("Reset optional config values related to general settings to be unset")]
            public static async Task ResetGeneralAsync(CommandContext context,
                [Description("The role a user needs to have to execute any basic commands (allows anyone when reset).")] SingleOption? BasicRole = null,
                [Description("The channel where commands, QOTDs and more get logged to (no logs are used when reset).")] SingleOption? LogsChannel = null
            )
            => await ResetAllAsync(context,
                BasicRole: BasicRole,
                LogsChannel: LogsChannel);

            [Command("qotd_sending")]
            [Description("Reset config values related to QOTD sending to be unset")]
            public static async Task ResetQotdSendingAsync(CommandContext context,
                [Description("Specifies on which days the QOTDs should get sent (sends daily when reset).")] SingleOption? TimeDayCondition = null)
            => await ResetAllAsync(context,
                QotdTimeDayCondition: TimeDayCondition);

            [Command("qotd_message")]
            [Description("Reset config values related to QOTD message appearance and behavior to be unset")]
            public static async Task ResetQotdMessageAsync(CommandContext context,
                [Description("The role that will get pinged when a new QOTD is sent (no role is pinged when reset).")] SingleOption? PingRole = null,
                [Description("Hex color code of the QOTD embed message. (defaults to \"#8acfac\" when reset).")] SingleOption? EmbedColorHex = null,
                [Description("The title that is displayed in QOTD messages. (defaults to \"Question Of The Day\" when reset).")] SingleOption? Title = null,
                [Description("The shorthand that is sometimes displayed in place of the title. (defaults to \"QOTD\" when reset).")] SingleOption? Shorthand = null)
            => await ResetAllAsync(context,
                QotdPingRole: PingRole,
                QotdTitle: Title,
                QotdShorthand: Shorthand,
                QotdEmbedColorHex: EmbedColorHex);

            [Command("suggestions")]
            [Description("Reset config values related to QOTD user suggestions to be unset")]
            public static async Task ResetSuggestionsAsync(CommandContext context,
                [Description("The channel new QOTD suggestions get announced in (no announcements are sent when reset).")] SingleOption? Channel = null,
                [Description("The role that will get pinged when a new QOTD is suggested (no role is pinged when reset).")] SingleOption? PingRole = null
            )
            => await ResetAllAsync(context, 
                SuggestionsChannel: Channel,
                SuggestionsPingRole: PingRole);
        }

        private static async Task ResetAllAsync(CommandContext context,
            SingleOption? BasicRole = null,
            SingleOption? QotdTimeDayCondition = null,
            SingleOption? QotdTitle = null,
            SingleOption? QotdShorthand = null,
            SingleOption? QotdEmbedColorHex = null,
            SingleOption? QotdPingRole = null,
            SingleOption? SuggestionsChannel = null,
            SingleOption? SuggestionsPingRole = null,
            SingleOption? LogsChannel = null)
        {
            if (!await Permissions.Api.Admin.UserHasAdministratorPermission(context))
                return;

            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);

            if (config is null)
                return;

            using (AppDbContext dbContext = new())
            {
                // Without extra retrieval config changes don't get saved
                config = await dbContext.Configs
                    .FindAsync(config.Id);

                if (config is null)
                    throw new Exception("Config not found");

                if (BasicRole is not null)
                    config.BasicRoleId = null;
                if (QotdTimeDayCondition is not null)
                {
                    config.QotdTimeDayCondition = null;
                    config.QotdTimeDayConditionLastChangedTimestamp = null;
                }
                if (QotdTitle is not null)  
                    config.QotdTitle = null;
                if (QotdShorthand is not null)
                    config.QotdShorthand = null;
                if (QotdEmbedColorHex is not null)
                    config.QotdEmbedColorHex = null;
                if (QotdPingRole is not null)
                    config.QotdPingRoleId = null;
                if (SuggestionsChannel is not null)
                    config.SuggestionsChannelId = null;
                if (SuggestionsPingRole is not null)
                    config.SuggestionsPingRoleId = null;
                if (LogsChannel is not null)
                    config.LogsChannelId = null;

                await dbContext.SaveChangesAsync();
            }

            QotdSending.Timer.Api.ConfigIdsToRecache.Add(config.Id);

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Success("Successfully set config values", $"{configString}"));
            Helpers.General.AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            await Logging.Api.LogUserAction(context, config, "Set config values", message: configString);
        }
    }
}
