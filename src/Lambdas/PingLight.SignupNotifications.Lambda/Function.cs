using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleSystemsManagement;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.SignupNotifications.Lambda;

public sealed class Function
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly NotificationWorkflow workflow;
    private readonly AccessChangeWorkflow accessChanges;
    private readonly Lazy<INotificationSender> sender;

    public Function()
    {
        var outbox = new DynamoNotificationOutbox(new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            MaxErrorRetry = 1, Timeout = TimeSpan.FromSeconds(2)
        }), Required("OUTBOX_TABLE"));
        workflow = new(outbox);
        accessChanges = new(outbox);
        // Delivery-only configuration is not required by the confirmation Lambda.
        sender = new(() => new NotificationSender(new AmazonSimpleEmailServiceClient(),
            new AmazonSimpleSystemsManagementClient(), Http,
            new(Required("STAGE"), Required("PROJECT_NAME"), Required("EMAIL_FROM"), Required("EMAIL_TO"),
                Required("TELEGRAM_CHAT_ID"), Required("TELEGRAM_TOKEN_PARAMETER"), Required("DEVICES_URL"))));
    }

    public async Task<JsonObject> PostConfirmationAsync(JsonObject signup, ILambdaContext context)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(
            Math.Max(1, context.RemainingTime.TotalMilliseconds - 500)));
        return await workflow.ConfirmSignupAsync(signup, timeout.Token);
    }

    public async Task<BatchResponse> DeliverAsync(JsonObject stream, ILambdaContext context)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(
            Math.Max(1, context.RemainingTime.TotalMilliseconds - 1000)));
        return await workflow.DeliverAsync(stream, sender.Value,
            eventId => context.Logger.LogError($"Signup notification delivery failed: {eventId}"), timeout.Token);
    }

    public async Task<BatchResponse> AccessChangedAsync(JsonObject stream, ILambdaContext context)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(
            Math.Max(1, context.RemainingTime.TotalMilliseconds - 1000)));
        return await accessChanges.EnqueueAsync(stream,
            eventId => context.Logger.LogError($"Access change notification enqueue failed: {eventId}"), timeout.Token);
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Configure {name}.");
}
