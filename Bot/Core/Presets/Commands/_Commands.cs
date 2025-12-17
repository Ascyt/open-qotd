using DSharpPlus.Commands;

namespace OpenQotd.Core.Presets.Commands
{
    [Command("presets")]
    public sealed partial class Presets
    {
        public enum PresetsType
        {
            Active, Completed
        }
    }
}
