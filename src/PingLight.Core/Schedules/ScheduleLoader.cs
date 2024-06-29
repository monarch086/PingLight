using Amazon.Lambda.Core;

namespace PingLight.Core.Schedules
{
    public class ScheduleLoader
    {
        private const string CALENDAR_HOST = "https://shutdown-calendar.fly.dev";
        private readonly Dictionary<string, string> scheduleMap = new Dictionary<string, string>();
        private readonly HttpClient client = new HttpClient();
        private readonly FlyDevGroupResolver groupResolver;
        private readonly ILambdaLogger logger;

        public ScheduleLoader(FlyDevGroupResolver groupResolver, ILambdaLogger logger)
        {
            this.logger = logger;
            this.groupResolver = groupResolver;
        }

        public async Task<string> GetOrLoadScheduleAsync(string groupNumber)
        {
            if (scheduleMap.ContainsKey(groupNumber))
            {
                return scheduleMap[groupNumber];
            }

            var resolvedGroup = groupResolver.Resolve(groupNumber);

            if (resolvedGroup == null)
            {
                return string.Empty;
            }

            var sourceUrl = $"{CALENDAR_HOST}/calendar/{groupNumber}.ics";

            var response = await client.GetAsync(sourceUrl);

            logger.LogInformation($"Requesting schedule for {sourceUrl}: {response.StatusCode}.");

            string responseText = await response.Content.ReadAsStringAsync();

            scheduleMap.Add(groupNumber, responseText);

            return responseText;
        }
    }
}
