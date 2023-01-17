using System.Text.Json;
using System.Text.Json.Nodes;

namespace PingLight.Core.Config
{
    public static class ConfigExtensions
    {
        public static string GetEnvironment(this JsonObject input)
        {
            return input.Deserialize<ScheduledEventInput>()?.Environment ?? "test";
        }

        public static bool IsProduction(this JsonObject input)
        {
            return input.GetEnvironment() == "prod";
        }
    }
}
