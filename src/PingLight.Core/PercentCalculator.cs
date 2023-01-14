namespace PingLight.Core
{
    public static class PercentCalculator
    {
        private const int MINS_IN_DAY = 1440;

        public static int CalculateDailyPercents(int minutes)
        {
            return minutes * 100 / MINS_IN_DAY;
        }
    }
}
