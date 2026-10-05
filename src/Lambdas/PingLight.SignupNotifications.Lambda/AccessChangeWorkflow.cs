using System.Text;
using System.Text.Json.Nodes;

namespace PingLight.SignupNotifications.Lambda;

public sealed class AccessChangeWorkflow(INotificationOutbox outbox)
{
    public async Task<BatchResponse> EnqueueAsync(JsonObject stream, Action<string?> logFailure,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<BatchFailure>();
        foreach (var record in stream["Records"]?.AsArray() ?? [])
        {
            if (record?["eventName"]?.GetValue<string>() is not ("MODIFY" or "INSERT")) continue;
            var sequence = record["dynamodb"]?["SequenceNumber"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Missing stream sequence number.");
            try
            {
                var oldImage = record["dynamodb"]?["OldImage"];
                var newImage = record["dynamodb"]?["NewImage"];
                var before = GrantKeys(oldImage);
                var after = GrantKeys(newImage);
                var changes = after.Except(before).Order(StringComparer.Ordinal).Select(key => Decode(key, true))
                    .Concat(before.Except(after).Order(StringComparer.Ordinal).Select(key => Decode(key, false))).ToArray();
                if (changes.Length == 0) continue;

                var email = newImage?["Email"]?["S"]?.GetValue<string>();
                var userId = newImage?["UserId"]?["S"]?.GetValue<string>();
                var eventId = record["eventID"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(eventId))
                    throw new InvalidOperationException("Missing access notification identity.");

                // A stream event ID is stable across retries but unique for each actual change.
                await outbox.EnqueueAsync([
                    new($"access/{userId}/{eventId}", email, "email", Kind: "access-change", AccessChanges: changes)
                ], cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                logFailure(record["eventID"]?.GetValue<string>());
                failures.Add(new(sequence));
            }
        }
        return new(failures);
    }

    private static HashSet<string> GrantKeys(JsonNode? image) => new(
        image?["DeviceGrants"]?["SS"]?.AsArray().Select(value => value!.GetValue<string>()) ?? [], StringComparer.Ordinal);

    private static DeviceAccessChange Decode(string key, bool granted)
    {
        var separator = key.IndexOf('.');
        if (separator < 0) throw new FormatException("Invalid device grant key.");
        return new(DecodePart(key[..separator]), DecodePart(key[(separator + 1)..]), granted);
    }

    private static string DecodePart(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(
        value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '=')));
}
