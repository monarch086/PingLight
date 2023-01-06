namespace PingLight.Core
{
    public static class PercentCalculator
    {
        private const int MINS_IN_DAY = 1440;

        public static int CalculateDailyPercents(int minutes)
        {
            return minutes / MINS_IN_DAY * 100;
        }
    }
}
