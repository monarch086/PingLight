using PingLight.Core.Model;

namespace PingLight.Core
{
    public static class BlackoutCalculator
    {
        public static List<TimeSpan> Calculate(List<Change> changes, DateTime from, DateTime till)
        {
            var blackouts = new List<TimeSpan>();

            for (int i = 0; i < changes.Count; i++)
            {
                // if first change is light on
                if (i == 0 && changes[i].IsLight)
                {
                    blackouts.Add(changes[i].ChangeDate - from);
                }

                // if last change is light off
                else if (i == changes.Count - 1 && !changes[i].IsLight)
                {
                    blackouts.Add(till - changes[i].ChangeDate);
                }

                // just regular change
                else if (!changes[i].IsLight)
                {
                    blackouts.Add(changes[i + 1].ChangeDate - changes[i].ChangeDate);
                }
            }

            return blackouts;
        }

        public static List<TimeSpan> CalculatePerDay(List<Change> changes, DateTime from, DateTime till)
        {
            var blackouts = new List<TimeSpan>();
            var currentStart = from;
            var currentEnd = currentStart.AddDays(1);

            while (currentStart < till)
            {
                var relevantChanges = changes
                    .Where(x => x.ChangeDate >= currentStart && x.ChangeDate <= currentEnd)
                    .ToList();

                var prevChange = changes
                    .Where(x => x.ChangeDate < currentStart)
                    .LastOrDefault();

                if (relevantChanges.Any())
                {
                    blackouts.Add(Calculate(relevantChanges, currentStart, currentEnd).Combine());
                } else if (prevChange != null && !prevChange.IsLight)
                {
                    blackouts.Add(TimeSpan.FromDays(1));
                } else
                {
                    blackouts.Add(TimeSpan.Zero);
                }

                currentStart = currentEnd;
                currentEnd = currentEnd.AddDays(1);
            }

            return blackouts;
        }

        public static List<TimeSpan> ToLightPerDay(this List<TimeSpan> blackouts)
        {
            var day = TimeSpan.FromDays(1);

            return blackouts.Select(b => day - b).ToList();
        }
    }
}
