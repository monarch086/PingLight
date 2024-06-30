using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Persistence;
using PingLight.Core.Schedules;
using System.Text;
using Calendar = Ical.Net.Calendar;

namespace PingLight.Test
{
    internal class CalendarEventsTester
    {
        public async Task<string> LoadCalendarEventsAsync(ILambdaContext context, string stage, InputModel input)
        {
            var configsRepository = new ConfigsRepository(stage, context.Logger);
            var customScheduleLoader = new CustomScheduleLoader(configsRepository, context.Logger);

            var groupResolver = new TestGroupResolver();
            var scheduleLoader = new ScheduleLoader(groupResolver, context.Logger);

            var isCustom = bool.Parse(input.IsCustom);

            var icsSchedule = await (isCustom ? customScheduleLoader.GetOrLoadScheduleAsync(input.Group) : scheduleLoader.GetOrLoadScheduleAsync(input.Group));
            var sb = new StringBuilder();

            var calendar = Calendar.Load(icsSchedule);

            sb.AppendLine($"\nEvents count: {calendar.Events.Count()}, groupNumber: {input.Group}.");

            var testDate = DateTime.UtcNow.AddMinutes(int.Parse(input.Shift));
            sb.AppendLine($"Test date: {testDate} // {testDate.ToString("o")}");

            var periodMinutes = 10;

            var searchStart = testDate.AddMinutes(periodMinutes - 5).ToKyivTime();
            var searchEnd = testDate.AddMinutes(periodMinutes + 5).ToKyivTime();
            var occurrences = calendar.GetOccurrences(searchStart, searchEnd);

            sb.AppendLine($"Occurrences: {occurrences.Count()}");

            foreach (var occurrence in occurrences)
            {
                sb.AppendLine($"Occurrence period: {occurrence.Period.StartTime.AsUtc.ToKyivTime()} - {occurrence.Period.EndTime.AsUtc.ToKyivTime()}.");
            }

            var nextOcurrence = occurrences.FirstOrDefault(o => (o.Period.StartTime.AsUtc - testDate).TotalMinutes < (periodMinutes + 5) &&
                                                            (o.Period.StartTime.AsUtc - testDate).TotalMinutes > (periodMinutes - 5));

            var nextEvent = calendar.Events.FirstOrDefault(e => e.DtStart.AsUtc > testDate
                                                            && (e.DtStart.AsUtc - testDate).TotalMinutes < (periodMinutes + 5)
                                                            && (e.DtStart.AsUtc - testDate).TotalMinutes > (periodMinutes - 5));

            var lastEvent = calendar.Events.OrderBy(e => e.DtStart).LastOrDefault();

            if (nextOcurrence != null)
            {
                sb.AppendLine($"Next ocurrence: {nextOcurrence.Period.StartTime.AsUtc.ToKyivTime()} - {nextOcurrence.Period.EndTime.AsUtc.ToKyivTime()}.");
            }
            if (nextEvent != null)
            {
                sb.AppendLine($"Next event: {nextEvent.DtStart.AsUtc.ToKyivTime()} - {nextEvent.DtEnd.AsUtc.ToKyivTime()}.");
            }
            if (lastEvent != null)
            {
                sb.AppendLine($"Last event: {lastEvent.DtStart.AsUtc.ToKyivTime()} - {lastEvent.DtEnd.AsUtc.ToKyivTime()}.");
            }

            return sb.ToString();
        }
    }
}
