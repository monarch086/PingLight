using PingLight.Core.Model;

namespace PingLight.Core
{
    public class BlackoutCalculator
    {
        public static List<TimeSpan> Calculate(List<Change> changes)
        {
            var blackouts = new List<TimeSpan>();

            for (int i = 0; i < changes.Count; i++)
            {
                // if first change is light on
                if (i == 0 && changes[i].IsLight)
                {
                    blackouts.Add(changes[i].ChangeDate - changes[i].ChangeDate.Date);
                }

                // if last change is light off
                else if (i == changes.Count - 1 && !changes[i].IsLight)
                {
                    var nextDay = changes[i].ChangeDate.Date.AddDays(1);
                    blackouts.Add(nextDay - changes[i].ChangeDate);
                }

                // just regular change
                else if (!changes[i].IsLight)
                {
                    blackouts.Add(changes[i + 1].ChangeDate - changes[i].ChangeDate);
                }
            }

            return blackouts;
        }
    }
}
