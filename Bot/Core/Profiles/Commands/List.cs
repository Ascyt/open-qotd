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
        [Command("list")]
        [Description("List all profiles you have permission to view.")]
        public static async Task ListProfilesAsync(CommandContext context,
            [Description("The page of the listing (default 1).")] int page = 1)
        {
            bool hasAdmin = await Permissions.Api.Admin.UserHasAdministratorPermission(context, responseOnError: false);

            int selectedProfileId = await Api.GetSelectedOrDefaultProfileIdAsync(context.Guild!.Id, context.User!.Id);

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

                    KeyValuePair<Config, ViewableProfileType>[] elementsInPage = [.. viewableConfigs
                        .Skip((page - 1) * itemsPerPage)
                        .Take(itemsPerPage)];

                    PageInfo<KeyValuePair<Config, ViewableProfileType>> pageInfo = new()
                    {
                        Elements = elementsInPage,
                        CurrentPage = page,
                        ElementsPerPage = itemsPerPage,
                        TotalElementsCount = totalElements
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
    }
}
