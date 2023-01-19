namespace PingLight.Core
{
    public static class PercentCalculator
    {
        private const int MINS_IN_DAY = 1440;
        private const int MINS_IN_WEEK = 10080;

        public static int CalculateDailyPercents(int minutes)
        {
            return minutes * 100 / MINS_IN_DAY;
        }

        public static int CalculateWeeklyPercents(int minutes)
        {
            return minutes * 100 / MINS_IN_WEEK;
        }
    }
}
