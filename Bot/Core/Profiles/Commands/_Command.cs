using DSharpPlus.Commands;

namespace OpenQotd.Core.Profiles.Commands
{
    [Command("profiles")]
    public sealed partial class ProfilesCommand
    {
        private enum ViewableProfileType
        {
            Viewable,
            Switchable,
            Current
        }
    }
}
