using System.Text;

namespace PingLight.Core
{
    public static class TimeExtensions
    {
        // private static int[] items = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public static string getString(this TimeSpan timeSpan)
        {
            var stringBuilder = new StringBuilder();

            if (timeSpan.Days > 0) stringBuilder.Append($" {timeSpan.Days} днів");

            if (timeSpan.Hours > 0) stringBuilder.Append($" {timeSpan.Hours} годин");

            if (timeSpan.Minutes > 0) stringBuilder.Append($" {timeSpan.Minutes} хвилин");

            return stringBuilder.ToString();
        }

        public static TimeSpan combineTimespans(this IEnumerable<TimeSpan> timeSpans)
        {
            return timeSpans.Aggregate((a, b) => a.Add(b));
        }
    }
}
