using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Database;
using OpenQotd.Core.Helpers;
using System.ComponentModel;

namespace OpenQotd.Core.Profiles.Commands
{
    public sealed partial class ProfilesCommand
    {
        [Command("delete")]
        [Description("Irreversably delete the current selected profile, including all its associated data.")]
        public static async Task DeleteProfileAsync(CommandContext context)
        {
            if (!await Permissions.Api.Admin.CheckAdminPermissionAsync(context))
                return;

            Config? config = await Api.TryGetSelectedOrDefaultConfigAsync(context);
            if (config is null)
                return;

            Config defaultConfig = await Api.GetDefaultConfigAsync(context.Guild!.Id);
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
                    .AddEmbed(Helpers.GenericEmbeds.Info(title:"Profile Deletion Cancelled", message:"Profile deletion was cancelled because no response has been received within 30 seconds.")));
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

            QotdSending.Timer.Api.ConfigIdsToRemoveFromCache.Add(config.Id);

            await sentMessage.ModifyAsync(new DiscordMessageBuilder()
                .AddEmbed(GenericEmbeds.Success("Profile Deleted", $"The **{config.ProfileName}** profile has been successfully deleted.")));
        }
    }
}
