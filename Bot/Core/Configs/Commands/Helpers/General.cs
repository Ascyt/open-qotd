using DSharpPlus.Commands;
using DSharpPlus.Entities;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using OpenQotd.QotdSending;

namespace OpenQotd.Core.Configs.Commands.Helpers
{
    internal static class General
    {
        public static async Task SetAllAsync(CommandContext context, 
            string? ProfileName = null,
            DiscordRole? BasicRole = null,
            DiscordRole? AdminRole = null,
            DiscordChannel? QotdChannel = null,
            int? QotdTimeHourUtc = null,
            int? QotdTimeMinuteUtc = null,
            string? QotdTimeDayCondition = null,
            Config.AlterQuestionAfterSentOption? QotdAlterQuestionAfterSent = null,
            string? QotdEmbedColorHex = null,
            DiscordRole? QotdPingRole = null,
            string? QotdTitle = null,
            string? QotdShorthand = null,
            bool? EnableAutomaticQotd = null,
            bool? EnableQotdPinMessage = null,
            bool? EnableQotdCreateThread = null,
            bool? EnableQotdAutomaticPresets = null,
            bool? EnableQotdLastAvailableWarn = null,
            bool? EnableQotdUnavailableMessage = null,
            bool? EnableQotdShowInfoButton = null,
            bool? EnableQotdShowFooter = null,
            bool? EnableQotdShowCredit = null,
            bool? EnableQotdShowCounter = null,
            bool? EnableSuggestions = null,
            DiscordChannel? SuggestionsChannel = null,
            DiscordRole? SuggestionsPingRole = null,
            bool? EnableSuggestionsPinMessage = null,
            Config.NoticeLevel? NoticesLevel = null,
			bool? EnableDeletedToStash = null,
			DiscordChannel? LogsChannel = null)
        {
            if (!await Permissions.Api.Admin.UserHasAdministratorPermission(context))
                return;

            Config? config = await Profiles.Api.TryGetSelectedOrDefaultConfigAsync(context);

            if (config is null)
                return;

            if (QotdTimeMinuteUtc is not null)
                QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc.Value, 0, 59);
            if (QotdTimeHourUtc is not null)
                QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc.Value, 0, 23);

            if (QotdTimeDayCondition is not null && !await Validity.IsValidDayCondition(context, QotdTimeDayCondition))
                return;

            if (ProfileName is not null && !await Validity.IsProfileNameValid(context, ProfileName))
                return;
            if (QotdTitle is not null && !await Validity.IsQotdTitleValid(context, QotdTitle))
                return;
            if (QotdShorthand is not null && !await Validity.IsQotdShorthandValid(context, QotdShorthand))
                return;
            if (QotdEmbedColorHex is not null)
            {
                (bool valid, QotdEmbedColorHex) = await Validity.IsQotdEmbedColorHexValid(context, QotdEmbedColorHex);
                if (!valid)
                    return;
            }

            using (AppDbContext dbContext = new())
            {
                // Without extra retrieval config changes don't get saved
                config = await dbContext.Configs
                    .FindAsync(config.Id) ?? throw new Exception("Config not found");
                
                if (QotdTimeMinuteUtc is not null || QotdTimeHourUtc is not null)
                {
                    int currentDay = DateTime.UtcNow.Day;

                    if (config.LastSentTimestamp?.Day == currentDay)
                    {
                        await context.Channel!.SendMessageAsync(GenericEmbeds.Warning($"Since a {config.QotdShorthandText} has already been sent today, the next one will be sent tomorrow at the specified time.\n\n" +
                            $"*Use `/trigger` to send a {config.QotdShorthandText} anyways!*"));
                    }
                }

                if (ProfileName is not null)
                    config.ProfileName = ProfileName;
                if (BasicRole is not null) 
                    config.BasicRoleId = BasicRole.Id;
                if (AdminRole is not null)
                    config.AdminRoleId = AdminRole.Id;
                if (QotdChannel is not null)
                    config.QotdChannelId = QotdChannel.Id;
                if (QotdTitle is not null)
                    config.QotdTitle = QotdTitle;
                if (QotdShorthand is not null)
                    config.QotdShorthand = QotdShorthand;
                if (QotdEmbedColorHex is not null)
                    config.QotdEmbedColorHex = QotdEmbedColorHex;
                if (EnableAutomaticQotd is not null)
                    config.EnableAutomaticQotd = EnableAutomaticQotd.Value;
                if (EnableQotdPinMessage is not null)
                    config.EnableQotdPinMessage = EnableQotdPinMessage.Value;
                if (EnableQotdCreateThread is not null)
                    config.EnableQotdCreateThread = EnableQotdCreateThread.Value;
                if (EnableQotdAutomaticPresets is not null)
                    config.EnableQotdAutomaticPresets = EnableQotdAutomaticPresets.Value;
                if (EnableQotdLastAvailableWarn is not null)
                    config.EnableQotdLastAvailableWarn = EnableQotdLastAvailableWarn.Value;
                if (EnableQotdUnavailableMessage is not null)
                    config.EnableQotdUnavailableMessage = EnableQotdUnavailableMessage.Value;
                if (EnableQotdShowInfoButton is not null)
                    config.EnableQotdShowInfoButton = EnableQotdShowInfoButton.Value;
                if (EnableQotdShowFooter is not null)
                    config.EnableQotdShowFooter = EnableQotdShowFooter.Value;
                if (EnableQotdShowCredit is not null)
                    config.EnableQotdShowCredit = EnableQotdShowCredit.Value;
                if (EnableQotdShowCounter is not null)
                    config.EnableQotdShowCounter = EnableQotdShowCounter.Value;
                if (QotdTimeHourUtc is not null)
                    config.QotdTimeHourUtc = QotdTimeHourUtc.Value;
                if (QotdTimeMinuteUtc is not null)
                    config.QotdTimeMinuteUtc = QotdTimeMinuteUtc.Value;
                if (QotdTimeDayCondition is not null)
                {
                    config.QotdTimeDayCondition = QotdTimeDayCondition;
                    config.QotdTimeDayConditionLastChangedTimestamp = DateTime.UtcNow;
                }
                if (QotdAlterQuestionAfterSent is not null)
                    config.QotdAlterQuestionAfterSent = QotdAlterQuestionAfterSent.Value;
                if (QotdPingRole is not null)
                    config.QotdPingRoleId = QotdPingRole.Id; 
                if (EnableSuggestions is not null)
                    config.EnableSuggestions = EnableSuggestions.Value;
                if (SuggestionsChannel is not null)
                    config.SuggestionsChannelId = SuggestionsChannel.Id;
                if (SuggestionsPingRole is not null)
                    config.SuggestionsPingRoleId = SuggestionsPingRole.Id;
                if (EnableSuggestionsPinMessage is not null)
                    config.EnableSuggestionsPinMessage = EnableSuggestionsPinMessage.Value;
                if (NoticesLevel is not null)
                    config.NoticesLevel = NoticesLevel.Value;
                if (EnableDeletedToStash is not null)
					config.EnableDeletedToStash = EnableDeletedToStash.Value;
				if (LogsChannel is not null)
                    config.LogsChannelId = LogsChannel.Id;

                await dbContext.SaveChangesAsync();   
            }

            QotdSenderTimer.ConfigIdsToRecache.Add(config.Id);

            string configString = config.ToString();

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(GenericEmbeds.Success("Successfully set config values", $"{configString}"));
            AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            await Logging.Api.LogUserAction(context, config, "Set config values", message: configString);
        }

        
        public static void AddInfoButton(DiscordMessageBuilder builder, int profileId)
        {
            builder.AddActionRowComponent(
                new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"show-general-info-no-prompt/{profileId}",
                    "𝐢"
                    )
                );
        }
    }
}
