using System;
using System.Linq;
using System.Text;

namespace PingLight.Core
{
    public static class TimeExtensions
    {
        private static int[] items = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private static Dictionary<int[], string> DaysMap => new()
            {
                { new int[] { 0 }, "днів" },
                { items[2..15], "днів" },
                { new int[] { 1 }, "дня" }
            };

        private static Dictionary<int[], string> HoursMap => new()
            {
                { new int[] { 0 }, "годин" },
                { items[2..15], "годин" },
                { new int[] { 1 }, "години" }
            };

        private static Dictionary<int[], string> MinutesMap => new()
            {
                { new int[] { 0 }, "хвилин" },
                { items[2..15], "хвилин" },
                { new int[] { 1 }, "хвилини" }
            };

        private static Dictionary<int[], string> TimesMap => new()
            {
                { new int[] { 0 }, "разів" },
                { items[5..15], "разів" },
                { new int[] { 1 }, "раз" },
                { items[2..5], "рази" }
            };

        public static string getString(this TimeSpan timeSpan)
        {
            var stringBuilder = new StringBuilder();

            if (timeSpan.Days > 0) stringBuilder.Append($" {timeSpan.getDays()}");

            if (timeSpan.Hours > 0) stringBuilder.Append($" {timeSpan.getHours()}");

            if (timeSpan.Minutes > 0) stringBuilder.Append($" {timeSpan.getMinutes()}");

            return stringBuilder.ToString();
        }

        public static TimeSpan Combine(this IEnumerable<TimeSpan> timeSpans)
        {
            return timeSpans.Aggregate((a, b) => a.Add(b));
        }

        public static string getDays(this TimeSpan timeSpan)
        {
            return getItems(timeSpan.Days, DaysMap);
        }

        public static string getHours(this TimeSpan timeSpan)
        {
            return getItems(timeSpan.Hours, HoursMap);
        }

        public static string getTotalHours(this TimeSpan timeSpan)
        {
            return getItems((int)timeSpan.TotalHours, HoursMap);
        }

        public static string getMinutes(this TimeSpan timeSpan)
        {
            return getItems(timeSpan.Minutes, MinutesMap);
        }

        public static string getTimes(this int times)
        {
            return getItems(times, TimesMap);
        }

        private static string getItems(int items, Dictionary<int[], string> map)
        {
            if (items == 0) return string.Empty;

            var _items = items;
 
            while (_items > 14)
            {
                _items %= 10;
            }

            var name = map[map.Keys.Where(k => k.Contains(_items)).First()];

            return $"{items} {name}";
        }
    }
}
