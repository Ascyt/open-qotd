using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Profiles.Commands
{
    public sealed partial class ProfilesCommand
    {
        [Command("switchto")]
        [Description("Switch to an existing profile you have the AdminRole for.")]
        public static async Task SwitchToProfileAsync(CommandContext context,
            [Description("The profile to switch to.")][SlashAutoCompleteProvider<AutoCompleteProviders.SwitchableProfiles>] int Profile)
        {
            int profileId = Profile;

            Config? configToSelect = await Api.TryGetConfigAsync(context, profileId);
            if (configToSelect is null)
                return;

            // Check if user has permission to switch to that profile
            bool hasAdmin = await Permissions.Api.Admin.CheckAdminPermissionAsync(context, responseOnError: false);
            if (!hasAdmin && !context.Member!.Roles.Any(role => role.Id == configToSelect.AdminRoleId))
            {
                await (context as SlashCommandContext)!
                    .RespondAsync(
                    GenericEmbeds.Error($"You need to have the \"<@&{configToSelect.AdminRoleId}>\" role or Server Administrator permission to be able to switch to that profile."), 
                    ephemeral: true);
                return;
            }

            using (AppDbContext dbContext = new())
            {
                ProfileSelection? selection = await dbContext.ProfileSelections
                    .Where(gu => gu.GuildId == context.Guild!.Id && gu.UserId == context.User.Id)
                    .FirstOrDefaultAsync();

                if (selection is null)
                {
                    selection = new ProfileSelection()
                    {
                        GuildId = context.Guild!.Id,
                        UserId = context.User.Id,
                        SelectedProfileId = configToSelect.ProfileId
                    };
                    dbContext.ProfileSelections.Add(selection);
                }
                else
                {
                    selection.SelectedProfileId = configToSelect.ProfileId;
                    dbContext.ProfileSelections.Update(selection);
                }
                await dbContext.SaveChangesAsync();
            }

            await (context as SlashCommandContext)!
                .RespondAsync(
                GenericEmbeds.Success(title: "Profile Switched", message: $"You have switched to the **{configToSelect.ProfileName}** profile.")
                    );
        }
    }
}
