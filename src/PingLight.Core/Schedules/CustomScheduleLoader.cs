using Amazon.Lambda.Core;
using PingLight.Core.Persistence;

namespace PingLight.Core.Schedules
{
    public class CustomScheduleLoader
    {
        private readonly Dictionary<string, string> scheduleMap = new Dictionary<string, string>();
        private readonly HttpClient client = new HttpClient();
        private readonly ConfigsRepository configsRepository;
        private readonly ILambdaLogger logger;
        private readonly string CONFIG_GROUP_KEY = "CustomCalendars";

        public CustomScheduleLoader(ConfigsRepository configsRepository, ILambdaLogger logger)
        {
            this.configsRepository = configsRepository;
            this.logger = logger;
        }

        public async Task<string> GetOrLoadScheduleAsync(string calendarKey)
        {
            if (scheduleMap.ContainsKey(calendarKey))
            {
                return scheduleMap[calendarKey];
            }

            var sourceUrl = await configsRepository.GetAsync(CONFIG_GROUP_KEY, calendarKey);

            if (sourceUrl == null)
            {
                logger.Log($"Failed to load custom calendar config for the key: {calendarKey}.");
                return string.Empty;
            }

            var response = await client.GetAsync(sourceUrl);

            logger.LogInformation($"Requesting schedule for {sourceUrl}: {response.StatusCode}.");

            string responseText = await response.Content.ReadAsStringAsync();

            scheduleMap.Add(calendarKey, responseText);

            return responseText;
        }
    }
}
