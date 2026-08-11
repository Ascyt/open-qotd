using static OpenQotd.Core.QotdSending.Timer.TimeCalculations;

namespace Tests.QotdSending
{
    public class QotdSendingTimeCalculationsTests
    {
        [Fact]
        public void Daily_WhenNotSentToday_Returns_TodayAtConfiguredTime()
        {
            DateTime now = new(2025, 10, 26, 10, 0, 0, DateTimeKind.Utc);
            DateTime? lastSent = null;
            DateTime expected = new(2025, 10, 26, 12, 30, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 12, 30, qotdTimeDayCondition: null, qotdTimeDayConditionLastChanged: null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void Daily_WhenAlreadySentToday_Returns_TomorrowAtConfiguredTime()
        {
            DateTime now = new(2025, 10, 26, 18, 0, 0, DateTimeKind.Utc);
            DateTime lastSent = new(2025, 10, 26, 8, 0, 0, DateTimeKind.Utc); // same day
            DateTime expected = new(2025, 10, 27, 9, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 9, 0, qotdTimeDayCondition: null, qotdTimeDayConditionLastChanged: null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void InvalidCondition_FallsBackToDaily()
        {
            DateTime now = new(2025, 3, 15, 7, 0, 0, DateTimeKind.Utc);
            DateTime? lastSent = null;
            string invalid = "%x999"; // invalid condition -> fallback to daily
            DateTime expected = new(2025, 3, 15, 8, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 8, 0, invalid, qotdTimeDayConditionLastChanged: null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void EveryNDays_Returns_NextMultipleSinceEpoch()
        {
            // epoch 2025-10-01, n=3
            DateTime epoch = new(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = new(2025, 10, 10, 9, 0, 0, DateTimeKind.Utc); // 9 days since epoch -> multiple=Ceiling(9/3)=3 -> date = epoch + 9 = 2025-10-10
            string condition = "%D3";
            DateTime expected = new(2025, 10, 10, 15, 45, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSentTimestamp: null, qotdTimeHourUtc: 15, qotdTimeMinuteUtc: 45, qotdTimeDayCondition: condition, qotdTimeDayConditionLastChanged: epoch);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void EveryNDays_WhenAlreadySentToday_OnSameCycle_AddsN()
        {
            DateTime epoch = new(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = new(2025, 10, 10, 20, 0, 0, DateTimeKind.Utc);
            DateTime lastSent = new(2025, 10, 10, 10, 0, 0, DateTimeKind.Utc);
            string condition = "%D3";
            DateTime expected = new(2025, 10, 13, 6, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 6, 0, condition, epoch);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void DaysOfWeek_Returns_NextAllowedDayThisWeek()
        {
            // Allowed Monday(1), Wednesday(3), Friday(5)
            string condition = "%w1,3,5";

            // now is Tuesday (mapping should give currentDayOfWeek = 2) => next allowed is Wednesday (3)
            DateTime now = new(2025, 10, 7, 7, 0, 0, DateTimeKind.Utc); // 2025-10-07 is a Tuesday
            DateTime? lastSent = null;

            DateTime expected = new(2025, 10, 8, 12, 0, 0, DateTimeKind.Utc); // Wednesday
            DateTime next = GetNextSendTime(now, lastSent, 12, 0, condition, null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void DaysOfWeek_WhenAllowedIsToday_ButAlreadySent_SkipsToNextAllowed()
        {
            // Allowed: Tuesday (2) and Thursday(4)
            string condition = "%w2,4";

            // now is Tuesday
            DateTime now = new(2025, 10, 7, 18, 0, 0, DateTimeKind.Utc); // Tuesday
            DateTime lastSent = new(2025, 10, 7, 9, 0, 0, DateTimeKind.Utc); // already sent today

            // next allowed after Tuesday is Thursday (2025-10-09)
            DateTime expected = new(2025, 10, 9, 6, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 6, 0, condition, null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void EveryNthWeekOnMthDay_Returns_CandidateWeekStartWhenInCycle()
        {
            // weekN = 1, M = Monday (1) -> every week on Monday
            string condition = "%W1;1";

            // Choose epoch equal to current week start so candidate is this week Monday
            DateTime now = new(2025, 10, 13, 8, 0, 0, DateTimeKind.Utc); // Monday
            DateTime epoch = new(2025, 10, 13, 0, 0, 0, DateTimeKind.Utc); // same week start
            DateTime? lastSent = null;

            DateTime expected = new(2025, 10, 13, 9, 30, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 9, 30, condition, epoch);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void EveryNthWeekOnMthDay_WhenAlreadySent_TakesNextCycle()
        {
            // weekN = 1, M = Monday (1) -> every week on Monday
            string condition = "%W1;1";

            DateTime now = new(2025, 10, 13, 18, 0, 0, DateTimeKind.Utc); // Monday
            DateTime epoch = new(2025, 10, 13, 0, 0, 0, DateTimeKind.Utc);
            DateTime lastSent = new(2025, 10, 13, 6, 0, 0, DateTimeKind.Utc); // already sent today

            // next cycle is one week later
            DateTime expected = new(2025, 10, 20, 7, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 7, 0, condition, epoch);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void DaysOfMonth_Returns_NextAllowedDayInCurrentMonth()
        {
            // allowed 15 and 1
            string condition = "%m15,1";

            DateTime now = new(2025, 10, 10, 6, 0, 0, DateTimeKind.Utc);
            DateTime? lastSent = null;

            DateTime expected = new(2025, 10, 15, 13, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 13, 0, condition, null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void DaysOfMonth_WhenTodayAndAlreadySent_Returns_NextAllowedInFollowingMonth()
        {
            // allowed days include today's day
            string condition = "%m10,20";

            DateTime now = new(2025, 10, 10, 14, 0, 0, DateTimeKind.Utc);
            DateTime lastSent = new(2025, 10, 10, 9, 0, 0, DateTimeKind.Utc); // already sent today

            // next allowed after skipping today is 20th of current month
            DateTime expected = new(2025, 10, 20, 9, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 9, 0, condition, null);

            Assert.Equal(expected, next);
        }

        [Fact]
        public void DaysOfMonth_SnapsToLastDayWhenMonthHasFewerDays()
        {
            // With snapping semantics: 31 in November (30 days) -> November 30
            string condition = "%m31,1";

            DateTime now = new(2025, 11, 15, 6, 0, 0, DateTimeKind.Utc); // November (30 days)
            DateTime? lastSent = null;

            // 31 snaps to 30 in Nov, and since it's still in the current month and >= today, expect Nov 30
            DateTime expected = new(2025, 11, 30, 8, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, lastSent, 8, 0, condition, null);

            Assert.Equal(expected, next);
        }

        [Theory]
        [InlineData(2025, 2, 10, 28)] // non-leap year -> Feb 28
        [InlineData(2024, 2, 10, 29)] // leap year     -> Feb 29
        public void DaysOfMonth_SnapsToFebEnd_ForRequested31_OnFeb(int year, int month, int dayNow, int expectedDom)
        {
            string condition = "%m31";
            DateTime now = new(year, month, dayNow, 6, 0, 0, DateTimeKind.Utc);
            DateTime expected = new(year, month, expectedDom, 8, 0, 0, DateTimeKind.Utc);

            DateTime next = GetNextSendTime(now, null, 8, 0, condition, null);

            Assert.Equal(expected, next);
        }
    }
}
