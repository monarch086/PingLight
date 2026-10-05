using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PingLight.SignupNotifications.Lambda;

public sealed record DeviceAccessChange(string DeviceId, string ChatId, bool Granted);
public sealed record SignupNotification(string Id, string Email, string Channel, bool Delivered = false,
    string Kind = "signup", IReadOnlyList<DeviceAccessChange>? AccessChanges = null);

public interface INotificationOutbox
{
    Task EnqueueAsync(IReadOnlyList<SignupNotification> notifications, CancellationToken cancellationToken);
    Task<SignupNotification?> LoadAsync(string id, CancellationToken cancellationToken);
    Task MarkDeliveredAsync(string id, CancellationToken cancellationToken);
}

public interface INotificationSender
{
    Task SendAsync(SignupNotification notification, CancellationToken cancellationToken);
}

// Preserve Cognito's full event when returning it, including fields unknown to this handler.
public sealed class NotificationWorkflow(INotificationOutbox outbox)
{
    public async Task<JsonObject> ConfirmSignupAsync(JsonObject signup, CancellationToken cancellationToken = default)
    {
        if (signup["triggerSource"]?.GetValue<string>() != "PostConfirmation_ConfirmSignUp") return signup;
        var email = signup["request"]?["userAttributes"]?["email"]?.GetValue<string>();
        var userId = signup["request"]?["userAttributes"]?["sub"]?.GetValue<string>();
        var poolId = signup["userPoolId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(poolId))
            throw new InvalidOperationException("Missing signup identity.");
        await outbox.EnqueueAsync([
            new($"{poolId}/{userId}/email", email, "email"),
            new($"{poolId}/{userId}/telegram", email, "telegram")
        ], cancellationToken);
        return signup;
    }

    public async Task<BatchResponse> DeliverAsync(JsonObject stream, INotificationSender sender,
        Action<string?> logFailure, CancellationToken cancellationToken = default)
    {
        var failures = new List<BatchFailure>();
        foreach (var record in stream["Records"]?.AsArray() ?? [])
        {
            if (record?["eventName"]?.GetValue<string>() != "INSERT") continue;
            var sequence = record["dynamodb"]?["SequenceNumber"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Missing stream sequence number.");
            try
            {
                var id = record["dynamodb"]?["Keys"]?["Id"]?["S"]?.GetValue<string>()
                    ?? throw new InvalidOperationException("Missing notification ID.");
                var notification = await outbox.LoadAsync(id, cancellationToken);
                if (notification is null || notification.Delivered) continue;
                await sender.SendAsync(notification, cancellationToken);
                await outbox.MarkDeliveredAsync(id, cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                logFailure(record["eventID"]?.GetValue<string>());
                failures.Add(new(sequence));
            }
        }
        return new(failures);
    }
}

public sealed record BatchResponse(
    [property: JsonPropertyName("batchItemFailures")] IReadOnlyList<BatchFailure> Failures);
public sealed record BatchFailure([property: JsonPropertyName("itemIdentifier")] string ItemIdentifier);
