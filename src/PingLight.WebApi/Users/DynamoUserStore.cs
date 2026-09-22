using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace PingLight.WebApi.Users;

public sealed class DynamoUserStore(IAmazonDynamoDB db, IConfiguration configuration) : IUserStore
{
    private readonly string table = configuration["Users:TableName"]
        ?? throw new InvalidOperationException("Configure Users:TableName.");

    public async Task<UserAccess> GetOrCreateAsync(string userId, string email, CancellationToken cancellationToken)
    {
        var response = await db.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = table,
            Key = new() { ["UserId"] = new(userId) },
            UpdateExpression = "SET Email = :email, IsSystemAdmin = if_not_exists(IsSystemAdmin, :notAdmin)",
            ExpressionAttributeValues = new()
            {
                [":email"] = new(email),
                [":notAdmin"] = new() { BOOL = false }
            },
            ReturnValues = ReturnValue.ALL_NEW
        }, cancellationToken);
        return Map(response.Attributes);
    }

    public async Task<bool> SetGrantAsync(string userId, string deviceId, string chatId, bool granted, CancellationToken cancellationToken)
    {
        var grant = new AttributeValue { SS = [UserGrantKey.Encode(deviceId, chatId)] };
        try
        {
            await db.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = table,
                Key = new() { ["UserId"] = new(userId) },
                ConditionExpression = "attribute_exists(UserId)",
                UpdateExpression = granted ? "ADD DeviceGrants :grant" : "DELETE DeviceGrants :grant",
                ExpressionAttributeValues = new() { [":grant"] = grant }
            }, cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException) { return false; }
    }

    private static UserAccess Map(IReadOnlyDictionary<string, AttributeValue> item) => new(
        item["UserId"].S,
        item.GetValueOrDefault("Email")?.S ?? "",
        item.GetValueOrDefault("IsSystemAdmin")?.BOOL ?? false,
        new HashSet<string>(item.GetValueOrDefault("DeviceGrants")?.SS ?? [], StringComparer.Ordinal));
}
