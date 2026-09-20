using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace PingLight.WebApi.Devices;

public sealed class DynamoDeviceStore : IDeviceStore
{
    private readonly IAmazonDynamoDB db;
    private readonly string table;

    public DynamoDeviceStore(IAmazonDynamoDB db, IConfiguration configuration)
    {
        this.db = db;
        table = configuration["Devices:TableName"] ?? throw new InvalidOperationException("Configure Devices:TableName.");
    }

    public async Task<DevicePage> ListAsync(string ownerId, CancellationToken cancellationToken)
    {
        Dictionary<string, AttributeValue>? startKey = null;
        var items = new List<DeviceView>();
        // Keep scan cursors on the server: they may identify other users' rows.
        // For the current small fleet, scan pages internally; add an owner index as it grows.
        do
        {
            var response = await db.ScanAsync(new ScanRequest
            {
                TableName = table,
                Limit = 100,
                ConsistentRead = true,
                ExclusiveStartKey = startKey,
                FilterExpression = "#owner = :owner",
                ProjectionExpression = "DeviceId, ChatId, Description, NotificationDelaySec, IsDailyStatsEnabled, IsWeeklyStatsEnabled, IsMonthlyStatsEnabled, IsActive",
                ExpressionAttributeNames = new() { ["#owner"] = "OwnerId" },
                ExpressionAttributeValues = new() { [":owner"] = new(ownerId) }
            }, cancellationToken);
            items.AddRange(response.Items.Select(item => new DeviceView(item["DeviceId"].S, item["ChatId"].S, new DeviceSettings
            {
                Description = item.GetValueOrDefault("Description")?.S ?? "",
                NotificationDelaySec = int.TryParse(item.GetValueOrDefault("NotificationDelaySec")?.N, out var delay) ? delay : 120,
                IsDailyStatsEnabled = item.GetValueOrDefault("IsDailyStatsEnabled")?.BOOL ?? false,
                IsWeeklyStatsEnabled = item.GetValueOrDefault("IsWeeklyStatsEnabled")?.BOOL ?? false,
                IsMonthlyStatsEnabled = item.GetValueOrDefault("IsMonthlyStatsEnabled")?.BOOL ?? false
            }, item.GetValueOrDefault("IsActive")?.BOOL ?? false)));
            startKey = response.LastEvaluatedKey;
        } while (startKey is { Count: > 0 });
        return new(items);
    }

    public async Task<bool> UpdateAsync(string ownerId, string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await db.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = table,
                Key = new() { ["DeviceId"] = new(deviceId), ["ChatId"] = new(chatId) },
                // Check ownership atomically; preserve ChatId and all other worker settings.
                ConditionExpression = "attribute_exists(DeviceId) AND #owner = :owner",
                UpdateExpression = "SET Description = :description, NotificationDelaySec = :delay, IsDailyStatsEnabled = :daily, IsWeeklyStatsEnabled = :weekly, IsMonthlyStatsEnabled = :monthly",
                ExpressionAttributeNames = new() { ["#owner"] = "OwnerId" },
                ExpressionAttributeValues = new()
                {
                    [":owner"] = new(ownerId),
                    [":description"] = new(settings.Description.Trim()),
                    [":delay"] = new() { N = settings.NotificationDelaySec.ToString(CultureInfo.InvariantCulture) },
                    [":daily"] = new() { BOOL = settings.IsDailyStatsEnabled },
                    [":weekly"] = new() { BOOL = settings.IsWeeklyStatsEnabled },
                    [":monthly"] = new() { BOOL = settings.IsMonthlyStatsEnabled }
                }
            }, cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException) { return false; }
    }
}
