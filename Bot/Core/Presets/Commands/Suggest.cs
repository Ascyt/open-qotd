using DSharpPlus.Commands;
using OpenQotd.Core.UncategorizedCommands;
using System.ComponentModel;

namespace OpenQotd.Core.Presets.Commands
{
    public sealed partial class PresetsCommand
    {
        [Command("suggest")]
        [Description("Suggest a preset to be added globally to OpenQOTD!")]
        public static async Task SuggestPresetAsync(CommandContext context,
            [Description("The QOTD question text to be suggested.")] string question)
            => await SimpleCommands.FeedbackAsync(context, $"Preset Suggestion: {question}");
    }
}
