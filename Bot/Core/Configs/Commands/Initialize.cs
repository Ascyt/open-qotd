using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Configs.Commands
{
    public sealed partial class ConfigCommand
    {
        [Command("initialize")]
        [Description("Initialize the config with values")]
        public static async Task InitializeAsync(CommandContext context,
            [Description("The role a user needs to have to execute admin commands (overrides BasicRole).")] DiscordRole AdminRole,
            [Description("The channel the QOTD should get sent in.")] DiscordChannel QotdChannel,
            [Description("The UTC hour of the day the QOTDs should get sent (0-23).")] int QotdTimeHourUtc,
            [Description("The UTC minute of the day the QOTDs should get sent (0-59).")] int QotdTimeMinuteUtc,            
            [Description("Whether to send a QOTD daily automatically (warning: may send a QOTD before setup is finished).")] bool EnableAutomaticQotd)
        {
            if (!await Permissions.Api.Admin.CheckAdminPermissionAsync(context))
                return;

            QotdTimeMinuteUtc = Math.Clamp(QotdTimeMinuteUtc, 0, 59);
            QotdTimeHourUtc = Math.Clamp(QotdTimeHourUtc, 0, 23);

            int existingConfigsCount;
            using (AppDbContext dbContext = new())
            {
                existingConfigsCount = await dbContext.Configs
                    .CountAsync(c => c.GuildId == context.Guild!.Id);
            }

            int profileId = await Profiles.Api.GetSelectedOrDefaultProfileIdAsync(context.Guild!.Id, context.Member!.Id);

            Config config = new()
            {
                GuildId = context!.Guild!.Id,
                ProfileId = profileId,
                IsDefaultProfile = existingConfigsCount == 0,
                ProfileName = Profiles.Api.GenerateProfileName(profileId),
                AdminRoleId = AdminRole.Id,
                QotdChannelId = QotdChannel.Id,
                QotdTimeHourUtc = QotdTimeHourUtc,
                QotdTimeMinuteUtc = QotdTimeMinuteUtc,
                EnableAutomaticQotd = EnableAutomaticQotd
            };
            bool reInitialized = false;

            using (AppDbContext dbContext = new())
            {
                Config? existingConfig = await dbContext.Configs
                    .FirstOrDefaultAsync(c => c.GuildId == context.Guild.Id && c.ProfileId == profileId);

                if (existingConfig != null)
                {
                    dbContext.Entry(existingConfig).State = EntityState.Detached; 
                    config.Id = existingConfig.Id;
                    dbContext.Configs.Update(config);
                    reInitialized = true;
                }
                else 
                {
                    await dbContext.Configs.AddAsync(config);
                }
                await dbContext.SaveChangesAsync();
            }
            string configString = config.ToString();

            QotdSending.Timer.Api.ConfigIdsToRecache.Add(config.Id);

            DiscordMessageBuilder builder = new();
            builder.AddEmbed(
                    GenericEmbeds.Success($"Successfully {(reInitialized ? "re-" : "")}initialized config", configString, profileName: config.ProfileName)
                    );
            Helpers.General.AddInfoButton(builder, config.ProfileId);

            await context.RespondAsync(builder);

            // Can cause issues
            // await LogUserAction(context, "Initialize config", configString);
        }
    }
}
