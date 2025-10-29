using OpenQotd.Database.Entities;

namespace OpenQotd.QotdSending
{
    public class QotdSenderTimeCalculations
    {
        public static DateTime GetNextSendTime(
            DateTime? lastSentTimestamp,
            int qotdTimeHourUtc,
            int qotdTimeMinuteUtc,
            string? qotdTimeDayCondition,
            DateTime? qotdTimeDayConditionLastChanged)
        {
            return GetNextSendTime(
                nowUtc: DateTime.UtcNow,
                lastSentTimestamp,
                qotdTimeHourUtc,
                qotdTimeMinuteUtc,
                qotdTimeDayCondition,
                qotdTimeDayConditionLastChanged
            );
        }

        public static DateTime GetNextSendTime(
            DateTime nowUtc,
            DateTime? lastSentTimestamp,
            int qotdTimeHourUtc,
            int qotdTimeMinuteUtc,
            string? qotdTimeDayCondition,
            DateTime? qotdTimeDayConditionLastChanged)
        {
            DateTime prev = lastSentTimestamp ?? DateTime.MinValue;
            DateTime now = nowUtc;

            bool alreadySentToday = prev.Ticks / TimeSpan.TicksPerDay == now.Ticks / TimeSpan.TicksPerDay;

            if (qotdTimeDayCondition is null) // send daily
            {
                DateTime todaySendTime = new(now.Year, now.Month, now.Day, qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc);

                if (alreadySentToday)
                {
                    return todaySendTime.AddDays(1);
                }

                return todaySendTime;
            }

            ParsedDayCondition parsedCondition = ParseDayConditionString(qotdTimeDayCondition);

            if (parsedCondition.Type == ParsedDayCondition.DayConditionType.Invalid)
            {
                // invalid condition, fallback to daily
                return GetNextSendTime(nowUtc, lastSentTimestamp, qotdTimeHourUtc, qotdTimeMinuteUtc, qotdTimeDayCondition: null, qotdTimeDayConditionLastChanged: null);
            }

            switch (parsedCondition.Type)
            {
                case ParsedDayCondition.DayConditionType.EveryNDays:
                    {
                        int n = parsedCondition.N;
                        DateTime epochDate = (qotdTimeDayConditionLastChanged?.Date ?? DateTime.MinValue.Date);
                        DateTime nowDateOnly = now.Date;

                        double daysSinceEpoch = (nowDateOnly - epochDate).TotalDays;
                        double multiples = Math.Ceiling(daysSinceEpoch / n);
                        DateTime nextDate = epochDate.AddDays(multiples * n);

                        DateTime nextSendTime = new(
                            nextDate.Year, nextDate.Month, nextDate.Day,
                            qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                        );

                        if (alreadySentToday && nextSendTime.Date == nowDateOnly)
                        {
                            nextSendTime = nextSendTime.AddDays(n);
                        }

                        return nextSendTime;
                    }

                case ParsedDayCondition.DayConditionType.DaysOfWeek:
                    {
                        int currentDayOfWeek = ((int)now.DayOfWeek + 6) % 7 + 1; // Monday=1, Sunday=7
                        IEnumerable<int> allowedDays = parsedCondition.AllowedDays.OrderBy(d => d);
                        foreach (int allowedDay in allowedDays)
                        {
                            if (allowedDay > currentDayOfWeek || (allowedDay == currentDayOfWeek && !alreadySentToday))
                            {
                                DateTime sendDate = now.AddDays(allowedDay - currentDayOfWeek);
                                return new(
                                    sendDate.Year, sendDate.Month, sendDate.Day,
                                    qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                                );
                            }
                        }
                        int firstAllowedDay = allowedDays.First();
                        DateTime nextWeekSendDate = now.AddDays(7 - currentDayOfWeek + firstAllowedDay);
                        return new(
                            nextWeekSendDate.Year, nextWeekSendDate.Month, nextWeekSendDate.Day,
                            qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                        );
                    }

                case ParsedDayCondition.DayConditionType.EveryNthWeekOnMthDay:
                    {
                        int weekN = Math.Max(1, parsedCondition.N);
                        int dayM = parsedCondition.M; // 1=Mon..7=Sun

                        DateTime epochDate = (qotdTimeDayConditionLastChanged?.Date ?? DateTime.MinValue.Date);
                        int epochDow = ((int)epochDate.DayOfWeek + 6) % 7 + 1; // Mon=1..Sun=7
                        DateTime epochWeekStart = epochDate.AddDays(1 - epochDow);

                        int nowDow = ((int)now.DayOfWeek + 6) % 7 + 1;
                        DateTime nowWeekStart = now.Date.AddDays(1 - nowDow);

                        long weeksSinceEpoch = (long)((nowWeekStart - epochWeekStart).TotalDays / 7);
                        long rem = ((weeksSinceEpoch % weekN) + weekN) % weekN;
                        long deltaWeeksToCycle = rem == 0 ? 0 : (weekN - rem);

                        DateTime candidateWeekStart = nowWeekStart.AddDays(deltaWeeksToCycle * 7);
                        DateTime candidateDate = candidateWeekStart.AddDays(dayM - 1);

                        if (candidateDate.Date < now.Date || (candidateDate.Date == now.Date && alreadySentToday))
                        {
                            candidateDate = candidateDate.AddDays(7L * weekN);
                        }

                        return new DateTime(
                            candidateDate.Year, candidateDate.Month, candidateDate.Day,
                            qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                        );
                    }
                case ParsedDayCondition.DayConditionType.DaysOfMonth:
                    {
                        int[] allowedDaysOfMonth = [.. parsedCondition.AllowedDays
                            .Distinct()
                            .OrderBy(d => d)];

                        DateTime today = now.Date;
                        int year = today.Year;
                        int month = today.Month;
                        int dim = DateTime.DaysInMonth(year, month);

                        // Try current month — snap requested day to last day if it exceeds month length
                        foreach (int d in allowedDaysOfMonth)
                        {
                            if (d < 1) continue;

                            int day = Math.Min(d, dim);
                            if (day < today.Day) continue;
                            if (day == today.Day && alreadySentToday) continue;

                            return new DateTime(
                                year, month, day,
                                qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                            );
                        }

                        // Try next 12 months — snap to last day per month as needed
                        for (int offset = 1; offset <= 12; offset++)
                        {
                            int m = month + offset;
                            int y = year + (m - 1) / 12;
                            m = ((m - 1) % 12) + 1;

                            int dimFuture = DateTime.DaysInMonth(y, m);
                            foreach (int d in allowedDaysOfMonth)
                            {
                                if (d < 1) continue;

                                int day = Math.Min(d, dimFuture);
                                return new DateTime(
                                    y, m, day,
                                    qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                                );
                            }
                        }

                        // Fallback (should rarely be hit with the snapping behavior)
                        int fallbackMonth = month == 12 ? 1 : month + 1;
                        int fallbackYear = month == 12 ? year + 1 : year;
                        return new DateTime(
                            fallbackYear, fallbackMonth, 1,
                            qotdTimeHourUtc, qotdTimeMinuteUtc, 0, DateTimeKind.Utc
                        );
                    }
            }

            // Fallback to daily (shouldn't reach here)
            return GetNextSendTime(nowUtc, lastSentTimestamp, qotdTimeHourUtc, qotdTimeMinuteUtc, qotdTimeDayCondition: null, qotdTimeDayConditionLastChanged: null);
        }

        public struct ParsedDayCondition
        {
            public enum DayConditionType
            {
                EveryNDays,
                DaysOfWeek,
                EveryNthWeekOnMthDay,
                DaysOfMonth,
                Invalid
            }
            public required DayConditionType Type;
            public int N; // for EveryNDays and EveryNthWeekOnMthDay
            public int M; // for EveryNthWeekOnMthDay
            public int[] AllowedDays; // for DaysOfWeek and DaysOfMonth
        }

        private static ParsedDayCondition ParseDayConditionString(string? dayCondition)
        {
            static ParsedDayCondition Invalid() 
                => new() { Type = ParsedDayCondition.DayConditionType.Invalid };

            if (string.IsNullOrWhiteSpace(dayCondition))
                return Invalid();

            if (dayCondition.Length < 3)
                return Invalid();

            if (!dayCondition.StartsWith('%')) 
                return Invalid();

            switch (dayCondition[1])
            {
                case 'D': // every n days
                    if (!int.TryParse(dayCondition[2..], out int n) || n <= 0)
                        return Invalid();

                    return new() { Type = ParsedDayCondition.DayConditionType.EveryNDays, N = n };

                case 'w': // days of the week starting with Monday=1
                    bool parsingFailed = false;
                    int[] allowedDaysOfWeek = [..dayCondition[2..].Split(',').Select(s => 
                    {
                        if (parsingFailed || !int.TryParse(s, out int day) || day < 1 || day > 7) 
                        {
                            parsingFailed = true;
                            return -1;
                        }
                        return day;
                    })];
                    if (parsingFailed)
                        return Invalid();

                    if (allowedDaysOfWeek.Length == 0)
                        return Invalid();

                    return new() { Type = ParsedDayCondition.DayConditionType.DaysOfWeek, AllowedDays = allowedDaysOfWeek };

                case 'W': // every nth week on the mth day of the week
                    string[] parts = dayCondition[2..].Split(';');
                    if (parts.Length != 2)
                        return Invalid();

                    if (!int.TryParse(parts[0], out int week) || week <= 0)
                        return Invalid();

                    if (!int.TryParse(parts[1], out int allowedDayOfWeek) || allowedDayOfWeek < 1 || allowedDayOfWeek > 7) 
                        return Invalid();

                    return new() { Type = ParsedDayCondition.DayConditionType.EveryNthWeekOnMthDay, N = week, M = allowedDayOfWeek };

                case 'm': // days of the month
                    bool parsingFailed1 = false;
                    int[] allowedDaysOfMonth = [..dayCondition[2..].Split(',').Select(s =>
                    {
                        if (parsingFailed1 || !int.TryParse(s, out int day) || day < 1 || day > 31)
                        {
                            parsingFailed1 = true;
                            return -1;
                        }
                        return day;
                    })];
                    if (parsingFailed1)
                        return Invalid();

                    if (allowedDaysOfMonth.Length == 0)
                        return Invalid();

                    return new() { Type = ParsedDayCondition.DayConditionType.DaysOfMonth, AllowedDays = allowedDaysOfMonth };

                default:
                    return Invalid();
            }
        }
    }
}
