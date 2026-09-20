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

    public async Task<DevicePage> ListAsync(IReadOnlySet<string>? grants, CancellationToken cancellationToken)
    {
        Dictionary<string, AttributeValue>? startKey = null;
        var items = new List<DeviceView>();
        // Keep scan cursors on the server: they may identify devices the caller cannot access.
        // For the current small fleet, scan pages internally; introduce an access index as it grows.
        do
        {
            var response = await db.ScanAsync(new ScanRequest
            {
                TableName = table,
                Limit = 100,
                ConsistentRead = true,
                ExclusiveStartKey = startKey,
                ProjectionExpression = "DeviceId, ChatId, Description, NotificationDelaySec, IsDailyStatsEnabled, IsWeeklyStatsEnabled, IsMonthlyStatsEnabled, IsActive",
            }, cancellationToken);
            items.AddRange(response.Items
                .Where(item => grants is null || grants.Contains(Users.UserGrantKey.Encode(item["DeviceId"].S, item["ChatId"].S)))
                .Select(item => new DeviceView(item["DeviceId"].S, item["ChatId"].S, new DeviceSettings
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

    public async Task<bool> ExistsAsync(string deviceId, string chatId, CancellationToken cancellationToken)
    {
        var response = await db.GetItemAsync(new GetItemRequest
        {
            TableName = table,
            Key = new() { ["DeviceId"] = new(deviceId), ["ChatId"] = new(chatId) },
            ConsistentRead = true,
            ProjectionExpression = "DeviceId"
        }, cancellationToken);
        return response.Item.Count > 0;
    }

    public async Task<bool> UpdateAsync(string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await db.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = table,
                Key = new() { ["DeviceId"] = new(deviceId), ["ChatId"] = new(chatId) },
                // Access is checked against the user grant before this call. Preserve all worker-only settings.
                ConditionExpression = "attribute_exists(DeviceId)",
                UpdateExpression = "SET Description = :description, NotificationDelaySec = :delay, IsDailyStatsEnabled = :daily, IsWeeklyStatsEnabled = :weekly, IsMonthlyStatsEnabled = :monthly",
                ExpressionAttributeValues = new()
                {
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
