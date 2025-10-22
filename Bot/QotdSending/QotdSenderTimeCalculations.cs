using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.QotdSending
{
    public class QotdSenderTimeCalculations
    {
        public static long GetNextSendTick(DateTime? lastSentTimestamp, int qotdTimeHourUtc, int qotdTimeMinuteUtc, string? qotdTimeDayCondition, DateTime? qotdTimeDayConditionLastChanged)
        {
            DateTime prev = lastSentTimestamp ?? DateTime.MinValue;
            DateTime now = DateTime.UtcNow;

            bool alreadySentToday = prev.Ticks / TimeSpan.TicksPerDay == now.Ticks / TimeSpan.TicksPerDay;

            return ticksUntilNextSend;
        }

        /// <summary>
        /// Returns true if the given day condition matches the given date time or is invalid.
        /// </summary>
        public static bool DoesDayConditionMatch(DateTime now, DateTime epoch, string? dayCondition)
        {
            const bool RETURN_VALUE_ON_INVALID = true;

            if (string.IsNullOrWhiteSpace(dayCondition))
                return RETURN_VALUE_ON_INVALID;

            if (dayCondition.Length < 3)
                return RETURN_VALUE_ON_INVALID;

            if (!dayCondition.StartsWith('#')) 
                return RETURN_VALUE_ON_INVALID;

            switch (dayCondition[1])
            {
                case 'D': // every n days
                    if (!int.TryParse(dayCondition[2..], out int n) || n < 0)
                    {
                        return RETURN_VALUE_ON_INVALID;
                    }
                    int daysSinceEpoch = (now - epoch).Days;
                    return daysSinceEpoch % n == 0;

                case 'w': // days of the week starting with Monday=1
                    bool parsingFailed = false;
                    int[] allowedDaysOfWeek = [..dayCondition[2..].Split(',').Select(s => 
                    {
                        if (!int.TryParse(s, out int day) || day < 1 || day > 7) 
                        {
                            parsingFailed = true;
                            return -1;
                        }
                        return day;
                    })];
                    if (parsingFailed)
                        return RETURN_VALUE_ON_INVALID;

                    // DateTime.DayOfWeek returns 0 for Sunday, 1 for Monday, ..., 6 for Saturday
                    int dayOfWeekNow = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
                    return allowedDaysOfWeek.Contains(dayOfWeekNow);

                case 'W': // every nth week on the mth day of the week
                    string[] parts = dayCondition[2..].Split(';');
                    if (parts.Length != 2)
                        return RETURN_VALUE_ON_INVALID;

                    if (!int.TryParse(parts[0], out int week) || week <= 0)
                        return RETURN_VALUE_ON_INVALID;

                    int weekIndex = (now - epoch).Days / 7;

                    if (weekIndex % week != 0)
                        return false;

                    if (!int.TryParse(parts[1], out int allowedDayOfWeek) && allowedDayOfWeek >= 1 && allowedDayOfWeek <= 7) 
                        return RETURN_VALUE_ON_INVALID;

                    // DateTime.DayOfWeek returns 0 for Sunday, 1 for Monday, ..., 6 for Saturday
                    int dayOfWeekNow1 = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
                    return dayOfWeekNow1 == allowedDayOfWeek;

                case 'm': // days of the month
                    bool parsingFailed1 = false;
                    int[] allowedDaysOfMonth = [..dayCondition[2..].Split(',').Select(s =>
                    {
                        if (!int.TryParse(s, out int day) || day < 1 || day > 31)
                        {
                            parsingFailed1 = true;
                            return -1;
                        }
                        return day;
                    })];
                    if (parsingFailed1)
                        return RETURN_VALUE_ON_INVALID;

                    return allowedDaysOfMonth.Contains(now.Day);

                default:
                    return RETURN_VALUE_ON_INVALID;
            }
        }
    }
}
