using DSharpPlus.Entities;

namespace OpenQotd.Bot.Helpers
{
    internal static class GenericEmbeds
    {
        public static DiscordEmbedBuilder Success(string title, string message, string? profileName = null) =>
            profileName is null ? 
                Custom(title, message, "#20c020") :
                Custom(title, message, "#20c020").WithFooter($"Profile: {profileName}");

        public static DiscordEmbedBuilder Error(string message, string title = "Error") =>
            Custom(title, message, "#ff0000");
        public static DiscordEmbedBuilder Warning(string message, string title = "Warning") =>
            Custom(title, message, "#ffc000");

        public static DiscordEmbedBuilder Custom(string title, string message, string color = "#5865f2") => new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithColor(new DiscordColor(color))
            .WithDescription(message);
    }
}
