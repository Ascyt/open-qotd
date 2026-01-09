using DSharpPlus.Commands;

namespace OpenQotd.Core.Presets.Commands
{
    [Command("presets")]
    public sealed partial class PresetsCommand
    {
        public enum PresetsType
        {
            Active, Completed
        }
    }
}
