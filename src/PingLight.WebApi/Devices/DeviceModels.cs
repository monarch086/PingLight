using System.ComponentModel.DataAnnotations;

namespace PingLight.WebApi.Devices;

public record DeviceSettings
{
    [Required(AllowEmptyStrings = true), StringLength(200)]
    public string Description { get; init; } = "";
    [Range(0, 86400)]
    public int NotificationDelaySec { get; init; } = 120;
    public bool IsDailyStatsEnabled { get; init; }
    public bool IsWeeklyStatsEnabled { get; init; }
    public bool IsMonthlyStatsEnabled { get; init; }
}

public record DeviceView(string DeviceId, string ChatId, DeviceSettings Settings, bool IsActive);
public record DevicePage(IReadOnlyList<DeviceView> Items);
public record NotificationState(bool IsActive);

public interface IDeviceStore
{
    Task<DevicePage> ListAsync(IReadOnlySet<string>? grants, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string deviceId, string chatId, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken);
    Task<bool> SetActiveAsync(string deviceId, string chatId, bool isActive, CancellationToken cancellationToken);
}
