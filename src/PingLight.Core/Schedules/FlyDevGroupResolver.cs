using Microsoft.Extensions.Configuration;

namespace PingLight.Core.Schedules
{
    public interface IGroupResolver
    {
        string? Resolve(string key);
    }

    public class FlyDevGroupResolver : IGroupResolver
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

    public class TestGroupResolver : IGroupResolver
    {
        public string? Resolve(string key)
        {
            switch (key)
            {
                case "1": return "1";
                case "2": return "1";
                case "3": return "2";
                case "4": return "2";
                case "5": return "3";
                default: return null;
            }
        }
    }
}
