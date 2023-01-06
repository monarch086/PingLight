using System.Text;

namespace PingLight.Core
{
    public static class MessageBuilder
    {
        private const string LIGHT_ICON = "✨";
        private const string EXCLAMATION_ICON = "‼";
        private const string STATS_ICON = "📊";

        public static string GetLightOnMessage(TimeSpan timeSpan)
        {
            return $"{LIGHT_ICON} Є світло!\nСвітло було відсутнє протягом{timeSpan.getString()}.";
        }

        public static string GetLightOffMessage(TimeSpan timeSpan)
        {
            return $"{EXCLAMATION_ICON} Нема світла((\nСвітло було протягом{timeSpan.getString()}.";
        }

        public static string GetDailyStatsMessage(List<TimeSpan> blackouts)
        {
            var sb = new StringBuilder($"{STATS_ICON} Щоденна статистика:\n");

            if (blackouts.Count == 0)
            {
                sb.Append("За минулу добу не зафіксовано відключень світла.");
                return sb.ToString();
            }

            var total = blackouts.combineTimespans();

            sb.Append($"За минулу добу світло було відключене {blackouts.Count} разів ");
            sb.Append($"на {total.Hours} годин {total.Minutes} хвилин.");

            return sb.ToString();
        }
    }
}
