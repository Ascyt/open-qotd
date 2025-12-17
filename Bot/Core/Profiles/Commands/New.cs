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
        [Command("new")]
        [Description("Switch to new-profile-mode. Then, use /config initialize to create the profile.")]
        public static async Task NewProfileAsync(CommandContext context)
        {
            if (!await Permissions.Api.Admin.UserHasAdministratorPermission(context))
                return;

            int? nextProfileId = await Config.TryGetNextProfileId(context.Guild!.Id);
            if (nextProfileId is null)
            {
                await context.RespondAsync(GenericEmbeds.Error("The default config must be initialized before a new profile can be created."));
                return;
            }

            using (AppDbContext dbContext = new())
            {
                // Check if within max profiles limit
                int existingProfilesCount = await dbContext.Configs
                    .Where(c => c.GuildId == context.Guild!.Id)
                    .CountAsync();
                if (existingProfilesCount >= Program.AppSettings.ConfigsPerGuildMaxAmount)
                {
                    await context.RespondAsync(GenericEmbeds.Error(
                        $"The maximum amount of profiles for this server ({Program.AppSettings.ConfigsPerGuildMaxAmount}) has been reached. It is not possible to create a new profile until an existing one is deleted."
                        ));
                    return;
                }

                ProfileSelection? selection = await dbContext.ProfileSelections
                .Where(gu => gu.GuildId == context.Guild!.Id && gu.UserId == context.User.Id)
                .FirstOrDefaultAsync();
                if (selection is null)
                {
                    selection = new ProfileSelection()
                    {
                        GuildId = context.Guild!.Id,
                        UserId = context.User.Id,
                        SelectedProfileId = nextProfileId.Value
                    };
                    dbContext.ProfileSelections.Add(selection);
                }
                else
                {
                    selection.SelectedProfileId = nextProfileId.Value;
                    dbContext.ProfileSelections.Update(selection);
                }
                await dbContext.SaveChangesAsync();
            }
            await context.RespondAsync(
                GenericEmbeds.Success(title:"Entered new-profile-mode", message:$"You have switched to **new-profile-mode**. Use `/config initialize` to create a new profile. Switching to a different profile will cancel new-profile-mode.")
                );
        }
    }
}
