using Microsoft.Extensions.Configuration;

namespace PingLight.Core.Schedules
{
    public class FlyDevGroupResolver
    {
        private readonly Dictionary<string, string> groupsMap = new Dictionary<string, string>();
        private readonly string CONFIG_SECTION = "FlyDevCalendarGroupsMap";

        public FlyDevGroupResolver(IConfiguration configuration)
        {
            for (int i = 1; i <= 5; i++)
            {
                if (!string.IsNullOrEmpty(configuration[$"{CONFIG_SECTION}:{i}"]))
                {
                    groupsMap.Add(i.ToString(), configuration[$"{CONFIG_SECTION}:{i}"]);
                }
            }
        }

        public string? Resolve(string key)
        {
            if (groupsMap.ContainsKey(key))
                return groupsMap[key];

            return null;
        }
    }
}
