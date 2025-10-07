using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using OpenQotd.Bot.Helpers.Profiles;
using System.ComponentModel;

namespace OpenQotd.Bot.Commands
{
    [Command("profiles")]
    public class ProfilesCommand
    {
        private enum ViewableProfileType
        {
            Viewable,
            Switchable,
            Current
        }

        [Command("list")]
        [Description("List all profiles you have permission to view.")]
        public static async Task ListProfilesAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            bool hasAdmin = await CommandRequirements.UserHasAdministratorPermission(context, responseOnError: false);

            int selectedProfileId = await ProfileHelpers.GetSelectedOrDefaultProfileIdAsync(context.Guild!.Id, context.User!.Id);

            Config[] configs = [];
            using (AppDbContext dbContext = new())
            {
                configs = await dbContext.Configs
                        .Where(c => c.GuildId == context.Guild!.Id)
                        .OrderByDescending(c => c.ProfileId == selectedProfileId) // Put selected profile first
                        .ThenByDescending(c => c.IsDefaultProfile) // Then put default profile second (or first if selected)
                        .ThenByDescending(c => c.Id) // Then by ID (newer profiles first)
                        .ToArrayAsync();
            }

            // key: config ID, value: whether the member has permission to switch to that profile
            Dictionary<Config, ViewableProfileType> viewableConfigs = [];
            if (hasAdmin)
            {
                // Server Admins can view and switch to all profiles
                viewableConfigs = configs.ToDictionary(c => c, c => (c.ProfileId == selectedProfileId ? ViewableProfileType.Current : ViewableProfileType.Switchable));
            }
            else
            {
                // Non-admins can only view profiles they have the basic/admin role of and switch to profiles they have the admin role of
                HashSet<ulong> userRoles = [.. context.Member!.Roles.Select(r => r.Id)];

                foreach (Config config in configs)
                {
                    bool hasAdminRole = userRoles.Contains(config.AdminRoleId);
                    bool hasAdminOrBasicRole = hasAdminRole || config.BasicRoleId is null || userRoles.Contains(config.BasicRoleId.Value);

                    if (!hasAdminOrBasicRole)
                        continue;

                    // Type current or switchable should only be set if the user has the admin role of the profile, even if it's their current profile, they can't run any admin commands anyways
                    viewableConfigs.Add(config, !hasAdminRole ? ViewableProfileType.Viewable : (config.ProfileId == selectedProfileId ? ViewableProfileType.Current : ViewableProfileType.Switchable));
                }
            }

            int itemsPerPage = Program.AppSettings.ListMessageItemsPerPage;
            await ListMessages.SendNew(context, page, $"{(hasAdmin ? "All" : "Viewable")} Profiles List",
                Task<PageInfo<KeyValuePair<Config, ViewableProfileType>>> (int page) =>
                {
                    int totalElements = viewableConfigs.Count;

                    int totalPages = (int)Math.Ceiling(totalElements / (double)itemsPerPage);

                    KeyValuePair<Config, ViewableProfileType>[] elementsInPage = [.. viewableConfigs
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    PageInfo<KeyValuePair<Config, ViewableProfileType>> pageInfo = new()
                    {
                        Elements = elementsInPage,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalElements,
                        TotalPagesCount = totalPages,
                    };

                    return Task.FromResult(pageInfo);
                }, ListProfileToString);
        }

        private static string ListProfileToString(KeyValuePair<Config, ViewableProfileType> viewableConfig, int rank)
        {
            string emoji = viewableConfig.Value switch 
            {
                ViewableProfileType.Viewable => ":black_small_square:",
                ViewableProfileType.Switchable => ":white_medium_square:",
                ViewableProfileType.Current => ":ballot_box_with_check:",
                _ => ":heavy_minus_sign:"
            };

            bool isCurrent = viewableConfig.Value == ViewableProfileType.Current;
            bool isDefault = viewableConfig.Key.IsDefaultProfile;

            return $"{emoji} {(isCurrent ? "**" : "")}{viewableConfig.Key.ProfileName}{(isCurrent ? "**" : "")}{(isDefault ? " (default)" : "")}{(isCurrent ? " (current)" : "")}";
        }

        [Command("new")]
        [Description("Switch to new-profile-mode. Then, use /config initialize to create the profile.")]
        public static async Task NewProfileAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
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

        [Command("switchto")]
        [Description("Switch to an existing profile you have the AdminRole for.")]
        public static async Task SwitchToProfileAsync(CommandContext context,
            [Description("The profile to switch to.")][SlashAutoCompleteProvider<SwitchableProfilesAutoCompleteProvider>] int Profile)
        {
            int profileId = Profile;

            Config? configToSelect = await ProfileHelpers.TryGetConfigAsync(context, profileId);
            if (configToSelect is null)
                return;

            // Check if user has permission to switch to that profile
            bool hasAdmin = await CommandRequirements.UserHasAdministratorPermission(context, responseOnError: false);
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

        [Command("setdefault")]
        [Description("Set the current profile as the default.")]
        public static async Task SetDefaultProfileAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
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

        [Command("delete")]
        [Description("Irreversably delete the current selected profile, including all its associated data.")]
        public static async Task DeleteProfileAsync(CommandContext context)
        {
            if (!await CommandRequirements.UserHasAdministratorPermission(context))
                return;

            Config? config = await ProfileHelpers.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            Config defaultConfig = await ProfileHelpers.GetDefaultConfigAsync(context.Guild!.Id);
            if (config.ProfileId == defaultConfig.ProfileId)
            {
                await context.RespondAsync(GenericEmbeds.Error($"The profile **{config.ProfileName}** is currently set as default. It is not possible to delete the default profile.\n" +
                    "You can change the default profile by switching to a different profile and running `/profiles setdefault`."));
                return;
            }

            DiscordMessageBuilder confirmMessage = new();
            confirmMessage.AddEmbed(GenericEmbeds.Warning(
                title: "Confirm Profile Deletion",
                message: $"Are you sure you want to delete the **{config.ProfileName}** profile?\n\n" +
                $"This action is **irreversable** and will delete all associated data, including questions, configuration, sent preset information, and more."));

            DiscordButtonComponent confirmButton = new(DiscordButtonStyle.Danger, "confirm_choice", "Irreversably Delete Profile And All Associated Data");
            DiscordButtonComponent cancelButton = new(DiscordButtonStyle.Secondary, "cancel_choice", "Cancel");

            confirmMessage.AddActionRowComponent(confirmButton, cancelButton);

            await context.RespondAsync(confirmMessage);

            DiscordMessage? sentMessage = await context.GetResponseAsync();
            if (sentMessage is null)
                return;

            InteractivityResult<ComponentInteractionCreatedEventArgs> result = await sentMessage!.WaitForButtonAsync(context.User, TimeSpan.FromSeconds(30));

            if (result.TimedOut)
            {
                await sentMessage.ModifyAsync(new DiscordMessageBuilder()
                    .AddEmbed(GenericEmbeds.Info(title:"Profile Deletion Cancelled", message:"Profile deletion was cancelled because no response has been received within 30 seconds.")));
                return;
            }

            switch (result.Result.Id)
            {
                case "confirm_choice":
                    break;
                case "cancel_choice":
                    await sentMessage.ModifyAsync(new DiscordMessageBuilder()
                        .AddEmbed(GenericEmbeds.Info(title:"Profile Deletion Cancelled", message:"The profile deletion has been cancelled.")));
                    return;
                default:
                    await sentMessage.ModifyAsync(new DiscordMessageBuilder()
                        .AddEmbed(GenericEmbeds.Error(title:"Profile Deletion Error", message:$"An unexpected error occurred while processing your response (unexpected result ID {result.Result.Id}). The profile deletion has been cancelled.")));
                    return;
            }

            using (AppDbContext dbContext = new())
            {
                // Delete profile selection for users that had this profile selected (switch to default profile)
                await dbContext.ProfileSelections
                    .Where(ps => ps.GuildId == context.Guild!.Id && ps.SelectedProfileId == config.ProfileId)
                    .ExecuteDeleteAsync();

                // Delete all questions associated with this profile
                await dbContext.Questions
                    .Where(q => q.ConfigId == config.Id)
                    .ExecuteDeleteAsync();

                // Delete all PresetSent entries associated with this profile
                await dbContext.PresetSents
                    .Where(ps => ps.ConfigId == config.Id)
                    .ExecuteDeleteAsync();

                // Finally, delete the profile itself
                dbContext.Configs.Remove(config);
                await dbContext.SaveChangesAsync();
            }

            await sentMessage.ModifyAsync(new DiscordMessageBuilder()
                .AddEmbed(GenericEmbeds.Success("Profile Deleted", $"The **{config.ProfileName}** profile has been successfully deleted.")));
        }
    }
}
