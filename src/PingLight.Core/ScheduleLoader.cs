using Amazon.Lambda.Core;

namespace PingLight.Core
{
    public class ScheduleLoader
    {
        private const string CALENDAR_HOST = "https://shutdown-calendar.fly.dev";
        private Dictionary<int, string> scheduleMap = new Dictionary<int, string>();
        private HttpClient client = new HttpClient();

        public async Task<string> GetOrLoadScheduleAsync(int groupNumber, ILambdaLogger logger)
        {
            if (scheduleMap.ContainsKey(groupNumber))
            {
                return scheduleMap[groupNumber];
            }

            var sourceUrl = $"{CALENDAR_HOST}/calendar/{groupNumber}.ics";
            
            HttpResponseMessage response = await client.GetAsync(sourceUrl);

            logger.LogInformation($"Requesting schedule for {sourceUrl}: {response.StatusCode}.");

            string responseText = await response.Content.ReadAsStringAsync();

            scheduleMap.Add(groupNumber, responseText);

            return responseText;
        }
    }
}
