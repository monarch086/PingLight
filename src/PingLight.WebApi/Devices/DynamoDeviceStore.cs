using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace PingLight.WebApi.Devices;

public sealed class DynamoDeviceStore : IDeviceStore
{
    private const int TurnOffPageSize = 10;
    private readonly IAmazonDynamoDB db;
    private readonly string table;
    private readonly string changesTable;

    public DynamoDeviceStore(IAmazonDynamoDB db, IConfiguration configuration)
    {
        this.db = db;
        table = configuration["Devices:TableName"] ?? throw new InvalidOperationException("Configure Devices:TableName.");
        changesTable = configuration["Changes:TableName"] ?? throw new InvalidOperationException("Configure Changes:TableName.");
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
        var latestByDevice = (await Task.WhenAll(items.Select(item => item.DeviceId).Distinct(StringComparer.Ordinal)
            .Select(async deviceId =>
                (DeviceId: deviceId, Period: (await ReadTurnOffRecordsAsync(deviceId, 1, 1, cancellationToken))
                    .Items.FirstOrDefault()?.Period))))
            .ToDictionary(result => result.DeviceId, result => result.Period, StringComparer.Ordinal);
        return new(items.Select(item => item with { LastTurnOff = latestByDevice[item.DeviceId] }).ToArray());
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

    public async Task<bool> SetActiveAsync(string deviceId, string chatId, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await db.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = table,
                Key = new() { ["DeviceId"] = new(deviceId), ["ChatId"] = new(chatId) },
                ConditionExpression = "attribute_exists(DeviceId)",
                UpdateExpression = "SET IsActive = :isActive",
                ExpressionAttributeValues = new()
                {
                    [":isActive"] = new() { BOOL = isActive }
                }
            }, cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException) { return false; }
    }

    public Task<TurnOffPage> ListTurnOffsAsync(string deviceId, int page, CancellationToken cancellationToken) =>
        ReadTurnOffsAsync(deviceId, page, cancellationToken);

    public async Task<bool> RemoveLastTurnOffAsync(string deviceId, CancellationToken cancellationToken)
    {
        var latest = (await ReadTurnOffRecordsAsync(deviceId, 1, 1, cancellationToken)).Items.FirstOrDefault();
        if (latest is null) return false;

        var writes = new List<TransactWriteItem> { DeleteChange(deviceId, latest.StartedAtKey, false) };
        if (latest.EndedAtKey is not null) writes.Add(DeleteChange(deviceId, latest.EndedAtKey, true));
        try
        {
            await db.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = writes }, cancellationToken);
            return true;
        }
        catch (TransactionCanceledException) { return false; }
    }

    private async Task<TurnOffPage> ReadTurnOffsAsync(string deviceId, int page, CancellationToken cancellationToken)
    {
        var result = await ReadTurnOffRecordsAsync(deviceId, page, TurnOffPageSize, cancellationToken);
        return new(result.Items.Select(item => item.Period).ToArray(), page, result.HasPreviousPage, result.HasNextPage);
    }

    private async Task<TurnOffRecordPage> ReadTurnOffRecordsAsync(string deviceId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var requiredPeriods = checked(page * pageSize + 1);
        var periods = new List<TurnOffRecord>(requiredPeriods);
        Dictionary<string, AttributeValue>? startKey = null;
        DateTimeOffset? pendingEnd = null;
        string? pendingEndKey = null;
        var isNewestChange = true;

        do
        {
            var response = await db.QueryAsync(new QueryRequest
            {
                TableName = changesTable,
                KeyConditionExpression = "DeviceId = :deviceId",
                ExpressionAttributeValues = new() { [":deviceId"] = new(deviceId) },
                ProjectionExpression = "ChangeDate, IsLight",
                ScanIndexForward = false,
                ConsistentRead = true,
                Limit = Math.Max(25, requiredPeriods * 2),
                ExclusiveStartKey = startKey
            }, cancellationToken);

            foreach (var item in response.Items)
            {
                var changedAt = DateTimeOffset.Parse(item["ChangeDate"].S, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                if (item["IsLight"].BOOL)
                {
                    pendingEnd = changedAt;
                    pendingEndKey = item["ChangeDate"].S;
                }
                else if (pendingEnd is not null)
                {
                    periods.Add(new(new(changedAt, pendingEnd), item["ChangeDate"].S, pendingEndKey));
                    pendingEnd = null;
                    pendingEndKey = null;
                }
                else if (isNewestChange)
                {
                    periods.Add(new(new(changedAt, null), item["ChangeDate"].S, null));
                }
                isNewestChange = false;
                if (periods.Count >= requiredPeriods) break;
            }
            startKey = response.LastEvaluatedKey;
        } while (periods.Count < requiredPeriods && startKey is { Count: > 0 });

        var offset = (page - 1) * pageSize;
        return new(periods.Skip(offset).Take(pageSize).ToArray(), page, page > 1, periods.Count > offset + pageSize);
    }

    private TransactWriteItem DeleteChange(string deviceId, string changeDate, bool isLight) => new()
    {
        Delete = new()
        {
            TableName = changesTable,
            Key = new() { ["DeviceId"] = new(deviceId), ["ChangeDate"] = new(changeDate) },
            ConditionExpression = "IsLight = :isLight",
            ExpressionAttributeValues = new() { [":isLight"] = new() { BOOL = isLight } }
        }
    };

    private sealed record TurnOffRecord(TurnOffPeriod Period, string StartedAtKey, string? EndedAtKey);
    private sealed record TurnOffRecordPage(IReadOnlyList<TurnOffRecord> Items, int Page,
        bool HasPreviousPage, bool HasNextPage);
}
