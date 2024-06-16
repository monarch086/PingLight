namespace PingLight.Core.DeviceConfig
{
    public class Config
    {
        public string DeviceId { get; set; }

        public string ChatId { get; set; }

        public string? Description { get; set; }

        public int? TurnOffGroup { get; set; }

        public bool IsDailyStatsEnabled { get; set; }

        public bool IsWeeklyStatsEnabled { get; set; }

        public bool IsMonthlyStatsEnabled { get; set; }
    }
}
