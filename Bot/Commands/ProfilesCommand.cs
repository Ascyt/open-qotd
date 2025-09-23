using OpenQotd.Bot.Database;
using OpenQotd.Bot.Database.Entities;
using OpenQotd.Bot.Helpers;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using DSharpPlus.Entities;

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

            Config[] configs = [];
            using (AppDbContext dbContext = new())
            {
                configs = await dbContext.Configs
                        .Where(c => c.GuildId == context.Guild!.Id)
                        .OrderBy(c => c.Id)
                        .ToArrayAsync();
            }
            int? selectedProfileId;
            using (AppDbContext dbContext = new())
            {
                selectedProfileId = await dbContext.GuildUsers
                    .Where(gu => gu.GuildId == context.Guild!.Id && gu.UserId == context.User.Id)
                    .Select(gu => (int?)gu.SelectedProfileId)
                    .FirstOrDefaultAsync();
            }
            selectedProfileId ??= 0;

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
                ViewableProfileType.Switchable => ":green_circle:",
                ViewableProfileType.Current => ":ballot_box_with_check:",
                _ => ":heavy_minus_sign:"
            };

            bool isCurrent = viewableConfig.Value == ViewableProfileType.Current;

            return $"{emoji} {(isCurrent ? "**" : "")}{viewableConfig.Key.ProfileName}{(isCurrent ? "**" : "")}";
        }
    }
}
