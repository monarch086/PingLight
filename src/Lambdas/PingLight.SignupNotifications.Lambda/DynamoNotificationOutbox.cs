using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace PingLight.SignupNotifications.Lambda;

public sealed class DynamoNotificationOutbox(IAmazonDynamoDB dynamo, string table) : INotificationOutbox
{
    public async Task EnqueueAsync(IReadOnlyList<SignupNotification> notifications, CancellationToken cancellationToken)
    {
        try
        {
            await dynamo.TransactWriteItemsAsync(new()
            {
                TransactItems = notifications.Select(notification => new TransactWriteItem
                {
                    Put = new()
                    {
                        TableName = table,
                        ConditionExpression = "attribute_not_exists(Id)",
                        Item = Serialize(notification)
                    }
                }).ToList()
            }, cancellationToken);
        }
        catch (TransactionCanceledException error) when (
            error.CancellationReasons?.Any(reason => reason.Code == "ConditionalCheckFailed") == true &&
            error.CancellationReasons.All(reason => reason.Code is "None" or "ConditionalCheckFailed"))
        {
            // Stream retries and repeated confirmations find the previously queued alerts.
        }
    }

    public async Task<SignupNotification?> LoadAsync(string id, CancellationToken cancellationToken)
    {
        var response = await dynamo.GetItemAsync(new()
        {
            TableName = table, Key = new() { ["Id"] = new(id) }, ConsistentRead = true
        }, cancellationToken);
        var item = response.Item;
        return item is null || item.Count == 0 ? null : new(
            item["Id"].S, item["Email"].S, item["Channel"].S, item["Delivered"].BOOL,
            item.GetValueOrDefault("Kind")?.S ?? "signup",
            item.GetValueOrDefault("AccessChanges")?.L.Select(change => new DeviceAccessChange(
                change.M["DeviceId"].S, change.M["ChatId"].S, change.M["Granted"].BOOL)).ToArray());
    }

    private static Dictionary<string, AttributeValue> Serialize(SignupNotification notification)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["Id"] = new(notification.Id), ["Email"] = new(notification.Email),
            ["Channel"] = new(notification.Channel), ["Delivered"] = new() { BOOL = false },
            ["Kind"] = new(notification.Kind)
        };
        if (notification.AccessChanges is not null)
            item["AccessChanges"] = new()
            {
                L = notification.AccessChanges.Select(change => new AttributeValue
                {
                    M = new()
                    {
                        ["DeviceId"] = new(change.DeviceId), ["ChatId"] = new(change.ChatId),
                        ["Granted"] = new() { BOOL = change.Granted }
                    }
                }).ToList()
            };
        return item;
    }

    public async Task MarkDeliveredAsync(string id, CancellationToken cancellationToken) =>
        await dynamo.UpdateItemAsync(new()
        {
            TableName = table, Key = new() { ["Id"] = new(id) },
            UpdateExpression = "SET Delivered = :yes",
            ExpressionAttributeValues = new() { [":yes"] = new() { BOOL = true } }
        }, cancellationToken);
}
