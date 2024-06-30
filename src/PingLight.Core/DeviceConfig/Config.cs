namespace PingLight.Core.DeviceConfig
{
    public class Config
    {
        public string DeviceId { get; set; }

        public string ChatId { get; set; }

        public string? Description { get; set; }

        public string? TurnOffGroup { get; set; }

        public int TurnOffPeriodMinutes { get; set; }

        public bool IsDailyStatsEnabled { get; set; }

        public bool IsWeeklyStatsEnabled { get; set; }

        public bool IsMonthlyStatsEnabled { get; set; }

        public int NotificationDelaySec { get; set; }

        public bool IsActive { get; set; }

        public bool UseCustomCalendar { get; set; }
    }
}
