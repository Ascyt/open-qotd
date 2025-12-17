using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Profiles.Commands
{
    public sealed partial class ProfilesCommand
    {
        [Command("setdefault")]
        [Description("Set the current profile as the default.")]
        public static async Task SetDefaultProfileAsync(CommandContext context)
        {
            if (!await Permissions.Api.Admin.UserHasAdministratorPermission(context))
                return;

            Config? config = await Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            using (AppDbContext dbContext = new())
            {
                // Remove default from existing default profile(s)
                await dbContext.Configs
                    .Where(c => c.GuildId == context.Guild!.Id && c.IsDefaultProfile)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.IsDefaultProfile, false));

                config.IsDefaultProfile = true;
                dbContext.Configs.Update(config);
                await dbContext.SaveChangesAsync();
            }

            await context.RespondAsync(
                GenericEmbeds.Success(title: "Default Profile Set", message: $"The **{config.ProfileName}** profile has been successfully set as the default profile.")
                );
        }
    }
}
