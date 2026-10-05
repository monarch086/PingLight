using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Moq;
using NUnit.Framework;
using PingLight.SignupNotifications.Lambda;

namespace PingLight.SignupNotifications.Tests;

public class NotificationTests
{
    private const string SignupJson = """
        {"triggerSource":"PostConfirmation_ConfirmSignUp","userPoolId":"pool",
         "request":{"userAttributes":{"sub":"user","email":"new@example.com"}},
         "response":{},"callerContext":{"clientId":"client"}}
        """;
    private static readonly SignupNotification Email = new("pool/user/email", "new@example.com", "email");
    private static readonly NotificationSettings Settings = new("dev", "PingLight", "sender@example.com",
        "sbarsuk88@gmail.com", "38627946", "/PingLight/dev/TelegramBot.Token", "https://dev.pinglight.xyz/devices");

    [Test]
    public async Task SignupPreservesCognitoEventAndQueuesStableChannelIds()
    {
        var serializer = new DefaultLambdaJsonSerializer();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(SignupJson));
        var signup = serializer.Deserialize<JsonObject>(input);
        IReadOnlyList<SignupNotification>? captured = null;
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.EnqueueAsync(It.IsAny<IReadOnlyList<SignupNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<SignupNotification>, CancellationToken>((items, _) => captured = items)
            .Returns(Task.CompletedTask);
        var workflow = new NotificationWorkflow(outbox.Object);
        Assert.That(await workflow.ConfirmSignupAsync(signup), Is.SameAs(signup));
        Assert.That(captured, Is.EqualTo(new[] { Email, Email with { Id = "pool/user/telegram", Channel = "telegram" } }));
        await workflow.ConfirmSignupAsync(signup);
        outbox.Verify(x => x.EnqueueAsync(It.Is<IReadOnlyList<SignupNotification>>(items => items[0].Id == Email.Id),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        using var output = new MemoryStream();
        serializer.Serialize(signup, output);
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(SignupJson), JsonNode.Parse(output.ToArray())), Is.True);
    }

    [TestCase("PostConfirmation_ConfirmForgotPassword")]
    [TestCase("PreSignUp_SignUp")]
    public async Task OtherTriggersDoNotNotify(string trigger)
    {
        var signup = JsonNode.Parse(SignupJson)!.AsObject();
        signup["triggerSource"] = trigger;
        var outbox = new Mock<INotificationOutbox>(MockBehavior.Strict);
        Assert.That(await new NotificationWorkflow(outbox.Object).ConfirmSignupAsync(signup), Is.SameAs(signup));
    }

    [Test]
    public void EnqueueFailuresPropagate()
    {
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.EnqueueAsync(It.IsAny<IReadOnlyList<SignupNotification>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unavailable"));
        Assert.ThrowsAsync<InvalidOperationException>(() => new NotificationWorkflow(outbox.Object)
            .ConfirmSignupAsync(JsonNode.Parse(SignupJson)!.AsObject()));
    }

    private static JsonObject Stream(params (string Id, string Sequence, string Event)[] records) => new()
    {
        ["Records"] = new JsonArray(records.Select(record => (JsonNode)new JsonObject
        {
            ["eventName"] = record.Event, ["eventID"] = record.Id,
            ["dynamodb"] = new JsonObject
            {
                ["SequenceNumber"] = record.Sequence,
                ["Keys"] = new JsonObject { ["Id"] = new JsonObject { ["S"] = record.Id } }
            }
        }).ToArray())
    };

    [Test]
    public async Task FailedEmailDoesNotPreventTelegramAndReturnsAwsPartialFailureShape()
    {
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => Email with { Id = id, Channel = id });
        var sender = new Mock<INotificationSender>();
        sender.Setup(x => x.SendAsync(It.Is<SignupNotification>(n => n.Channel == "email"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SES unavailable"));
        var failures = new List<string?>();
        var result = await new NotificationWorkflow(outbox.Object).DeliverAsync(
            Stream(("email", "1", "INSERT"), ("telegram", "2", "INSERT")), sender.Object, failures.Add);
        outbox.Verify(x => x.MarkDeliveredAsync("telegram", It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(x => x.MarkDeliveredAsync("email", It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(failures, Is.EqualTo(new[] { "email" }));
        using var output = new MemoryStream();
        new DefaultLambdaJsonSerializer().Serialize(result, output);
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(output.ToArray()),
            JsonNode.Parse("""{"batchItemFailures":[{"itemIdentifier":"1"}]}""")), Is.True);
    }

    [Test]
    public async Task CompletedAlertsAndUpdateEventsAreSkipped()
    {
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.LoadAsync("done", It.IsAny<CancellationToken>())).ReturnsAsync(Email with { Delivered = true });
        var sender = new Mock<INotificationSender>(MockBehavior.Strict);
        var result = await new NotificationWorkflow(outbox.Object).DeliverAsync(
            Stream(("done", "1", "INSERT"), ("update", "2", "MODIFY")), sender.Object,
            _ => Assert.Fail("Unexpected failure"));
        Assert.That(result.Failures, Is.Empty);
        outbox.Verify(x => x.LoadAsync("update", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OutboxAtomicallyQueuesBothChannelsAndDeduplicatesOnlyConditionalFailures()
    {
        var dynamo = new Mock<IAmazonDynamoDB>();
        TransactWriteItemsRequest? captured = null;
        dynamo.Setup(x => x.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransactWriteItemsRequest, CancellationToken>((request, _) => captured = request)
            .ThrowsAsync(new TransactionCanceledException("duplicate")
            {
                CancellationReasons = [new() { Code = "ConditionalCheckFailed" }, new() { Code = "ConditionalCheckFailed" }]
            });
        var outbox = new DynamoNotificationOutbox(dynamo.Object, "outbox");
        await outbox.EnqueueAsync([Email, Email with { Id = "telegram", Channel = "telegram" }], default);
        Assert.That(captured!.TransactItems.Count, Is.EqualTo(2));
        Assert.That(captured.TransactItems.All(item => item.Put.ConditionExpression == "attribute_not_exists(Id)"), Is.True);
        dynamo.Setup(x => x.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransactionCanceledException("unavailable")
            {
                CancellationReasons = [new() { Code = "ProvisionedThroughputExceeded" }]
            });
        Assert.ThrowsAsync<TransactionCanceledException>(() => outbox.EnqueueAsync([Email], default));
    }

    [TestCase("dev")]
    [TestCase("prod")]
    public async Task SesRequestContainsRecipientProjectStageAndUserEmail(string stage)
    {
        SendEmailRequest? captured = null;
        var ses = new Mock<IAmazonSimpleEmailService>();
        ses.Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SendEmailResponse());
        using var http = new HttpClient();
        await new NotificationSender(ses.Object, Mock.Of<IAmazonSimpleSystemsManagement>(), http,
            Settings with { Stage = stage }).SendAsync(Email, default);
        Assert.That(captured!.Destination.ToAddresses, Is.EqualTo(new[] { "sbarsuk88@gmail.com" }));
        Assert.That(captured.Message.Body.Text.Data, Is.EqualTo(
            $"Зареєструвався новий користувач\nСередовище: {stage}\nПроєкт: PingLight\nЕлектронна пошта користувача: new@example.com"));
        Assert.That(captured.Message.Subject.Data, Is.EqualTo($"[{stage}] PingLight: новий користувач"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task TelegramUsesDecryptedTokenAndPlainTextAndRejectsApiFailure(bool ok)
    {
        var ssm = new Mock<IAmazonSimpleSystemsManagement>();
        ssm.Setup(x => x.GetParameterAsync(It.Is<GetParameterRequest>(request =>
            request.Name == Settings.TelegramTokenParameter && request.WithDecryption), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParameterResponse { Parameter = new Parameter { Value = "test-token" } });
        JsonObject? payload = null;
        using var http = new HttpClient(new StubHttpHandler(async request =>
        {
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/bottest-token/sendMessage"));
            payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync())!.AsObject();
            return new(HttpStatusCode.OK) { Content = new StringContent(ok ? "{\"ok\":true}" : "{\"ok\":false}") };
        }));
        var sender = new NotificationSender(Mock.Of<IAmazonSimpleEmailService>(), ssm.Object, http, Settings);
        var notification = Email with { Channel = "telegram" };
        if (ok) await sender.SendAsync(notification, default);
        else Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(notification, default));
        Assert.That(payload!["chat_id"]!.GetValue<string>(), Is.EqualTo("38627946"));
        Assert.That(payload["text"]!.GetValue<string>(), Is.EqualTo(NotificationSender.MessageFor(Email, Settings)));
        Assert.That(payload.ContainsKey("parse_mode"), Is.False);
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request);
    }
}
