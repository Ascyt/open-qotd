using DSharpPlus.Commands;
using OpenQotd.Core.Helpers;

namespace OpenQotd.Core.Configs.Commands.Helpers
{
    internal static class Validity
    {   
        /// <summary>
        /// Checks whether or not the <paramref name="qotdTitle"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        internal static async Task<bool> IsQotdTitleValid(CommandContext context, string qotdTitle)
        {
            if (qotdTitle.Length > Program.AppSettings.ConfigQotdTitleMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided QOTD Title must not exceed {Program.AppSettings.ConfigQotdTitleMaxLength} characters in length (provided length is {qotdTitle.Length}).")
                    );
                return false;
            }

            if (qotdTitle.Contains('\n'))
            {
                await context.RespondAsync($"The provided QOTD title must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether or not the <paramref name="qotdShorthand"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        internal static async Task<bool> IsQotdShorthandValid(CommandContext context, string qotdShorthand)
        {
            if (qotdShorthand.Length > Program.AppSettings.ConfigQotdShorthandMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided QOTD shorthand must not exceed {Program.AppSettings.ConfigQotdShorthandMaxLength} characters in length (provided length is {qotdShorthand.Length}).")
                    );
                return false;
            }

            if (qotdShorthand.Contains('\n'))
            {
                await context.RespondAsync($"The provided QOTD shorthand must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether or not the <paramref name="profileName"/> is within valid length (provided by <see cref="AppSettings.ConfigQotdTitleMaxLength"/>)
        /// and does not contain any forbidden characters.
        /// </summary>
        internal static async Task<bool> IsProfileNameValid(CommandContext context, string profileName)
        {
            if (profileName.Length > Program.AppSettings.ConfigProfileNameMaxLength)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided profile name must not exceed {Program.AppSettings.ConfigProfileNameMaxLength} characters in length (provided length is {profileName.Length}).")
                    );
                return false;
            }

            if (profileName.Contains('\n'))
            {
                await context.RespondAsync($"The provided profile name must not contain any line-breaks.");
                return false;
            }

            return true;
        }

        internal static async Task<bool> IsValidDayCondition(CommandContext context, string dayCondition)
        {
            if (dayCondition.Length > 64)
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided day condition must not exceed 64 characters in length (provided length is {dayCondition.Length}).")
                    );
                return false;
            }

            if (!IsValidDayConditionFormat(dayCondition))
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided day condition (`{dayCondition}`) is invalid. Please refer to the documentation for valid formats.")
                    );
                return false;
            }
            return true;
        }
        /// <returns>(is valid, new hex code)</returns>
        internal static async Task<(bool, string)> IsQotdEmbedColorHexValid(CommandContext context, string qotdEmbedColorHex)
        {
            if (!IsValidHexCode(ref qotdEmbedColorHex))
            {
                await context.RespondAsync(
                    GenericEmbeds.Error($"The provided QOTD embed color hex code (`{qotdEmbedColorHex}`) is invalid. Please provide a valid hex code in the format `#RRGGBB`.")
                    );
                return (false, "");
            }
            return (true, qotdEmbedColorHex);
        }

        internal static bool IsValidHexCode(ref string hexCode)
        {
            if (!hexCode.StartsWith('#'))
                hexCode = "#" + hexCode;

            if (hexCode.Length != 7)
                return false;

            hexCode = hexCode.ToLowerInvariant();

            for (int i = 1; i < hexCode.Length; i++)
            {
                char c = hexCode[i];
                bool isHexDigit = (c >= '0' && c <= '9') ||
                                (c >= 'a' && c <= 'f');
                if (!isHexDigit)
                    return false;
            }

            return true;
        }

        private static bool IsValidDayConditionFormat(string dayCondition)
        {
            if (dayCondition.Length < 2)
                return false;

            if (!dayCondition.StartsWith('%'))
                return false;
            
            switch (dayCondition[1])
            {
                case 'D': // Every 'n' days
                    if (dayCondition.Length < 3 || !int.TryParse(dayCondition[2..], out int n) || n < 1 || n > 31)
                        return false;
                    return true;
                case 'w': // Days of the week, starting with Monday=1
                    if (dayCondition.Length < 3)
                        return false;

                    string[] parts = dayCondition[2..].Split(',');
                    foreach (string part in parts)
                    {
                        if (!int.TryParse(part, out int day) || day < 1 || day > 7)
                            return false;
                    }

                    return true;
                case 'W': // Every nth week on the mth day of the week
                    if (dayCondition.Length < 5)
                        return false;
                    string[] parts1 = dayCondition[2..].Split(';');

                    if (parts1.Length != 2)
                        return false;

                    string weekIndexPart = parts1[0];
                    if (!int.TryParse(weekIndexPart, out int weekIndex) || weekIndex < 1)
                        return false;

                    string dayOfWeekPart = parts1[1];
                    if (!int.TryParse(dayOfWeekPart, out int dayOfWeek) || dayOfWeek < 1 || dayOfWeek > 7)
                        return false;
                    return true;
                case 'm': // Days of the month
                    if (dayCondition.Length < 3)
                        return false;

                    string[] parts2 = dayCondition[2..].Split(',');
                    foreach (string part in parts2)
                    {
                        if (!int.TryParse(part, out int day) || day < 1 || day > 31)
                            return false;
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
