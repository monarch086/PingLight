using System.Text;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleSystemsManagement;
using Moq;
using NUnit.Framework;
using PingLight.SignupNotifications.Lambda;

namespace PingLight.SignupNotifications.Tests;

public class AccessChangeTests
{
    private static string Key(string device, string chat) => $"{Encode(device)}.{Encode(chat)}";
    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static JsonObject Image(params string[] keys) => new()
    {
        ["UserId"] = new JsonObject { ["S"] = "user" },
        ["Email"] = new JsonObject { ["S"] = "recipient@example.com" },
        ["DeviceGrants"] = new JsonObject { ["SS"] = new JsonArray(keys.Select(key => (JsonNode)JsonValue.Create(key)!).ToArray()) }
    };

    private static JsonObject Record(string eventId, string sequence, JsonObject before, JsonObject after,
        string eventName = "MODIFY") => new()
    {
        ["eventID"] = eventId, ["eventName"] = eventName,
        ["dynamodb"] = new JsonObject { ["SequenceNumber"] = sequence, ["OldImage"] = before, ["NewImage"] = after }
    };

    private static JsonObject Stream(params JsonObject[] records) => new() { ["Records"] = new JsonArray(records) };

    [Test]
    public async Task ActualGrantAndRevokeChangesQueueOneEmailWithDecodedDevices()
    {
        IReadOnlyList<SignupNotification>? captured = null;
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.EnqueueAsync(It.IsAny<IReadOnlyList<SignupNotification>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<SignupNotification>, CancellationToken>((items, _) => captured = items)
            .Returns(Task.CompletedTask);
        var stream = Stream(Record("event-1", "1", Image(Key("removed", "123"), Key("kept", "0")),
            Image(Key("kept", "0"), Key("Дім / квартира", "-100123"))));
        var result = await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(stream, _ => Assert.Fail());
        Assert.That(result.Failures, Is.Empty);
        Assert.That(captured, Has.Count.EqualTo(1));
        var email = captured![0];
        Assert.That(email.Email, Is.EqualTo("recipient@example.com"));
        Assert.That(email.Kind, Is.EqualTo("access-change"));
        Assert.That(email.Channel, Is.EqualTo("email"));
        Assert.That(email.AccessChanges, Is.EqualTo(new[] {
            new DeviceAccessChange("Дім / квартира", "-100123", true), new DeviceAccessChange("removed", "123", false)
        }));
        var originalId = email.Id;
        await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(stream, _ => Assert.Fail());
        Assert.That(captured![0].Id, Is.EqualTo(originalId));
        stream["Records"]![0]!["eventID"] = "event-2";
        await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(stream, _ => Assert.Fail());
        Assert.That(captured![0].Id, Is.Not.EqualTo(originalId));
    }

    [Test]
    public async Task UnchangedSetsEmailUpdatesRoleUpdatesAndEmptyNewAccountsDoNotSend()
    {
        var before = Image(Key("a", "1"), Key("b", "2"));
        var after = Image(Key("b", "2"), Key("a", "1"));
        after["Email"]!["S"] = "updated@example.com";
        after["IsSystemAdmin"] = new JsonObject { ["BOOL"] = true };
        var stream = Stream(Record("unchanged", "1", before, after),
            Record("signup", "2", new(), Image(), "INSERT"));
        var result = await new AccessChangeWorkflow(new Mock<INotificationOutbox>(MockBehavior.Strict).Object)
            .EnqueueAsync(stream, _ => Assert.Fail());
        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public async Task RemovingLastGrantNotifiesAndMissingSetIsTreatedAsEmpty()
    {
        var after = Image();
        after.Remove("DeviceGrants");
        var outbox = new Mock<INotificationOutbox>();
        await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(
            Stream(Record("revoke", "1", Image(Key("last", "123")), after)), _ => Assert.Fail());
        outbox.Verify(x => x.EnqueueAsync(It.Is<IReadOnlyList<SignupNotification>>(items =>
            items[0].AccessChanges!.Count == 1 && !items[0].AccessChanges![0].Granted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task NewlyInsertedUserWithGrantsNotifies()
    {
        var outbox = new Mock<INotificationOutbox>();
        await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(
            Stream(Record("insert", "1", new(), Image(Key("device", "123")), "INSERT")), _ => Assert.Fail());
        outbox.Verify(x => x.EnqueueAsync(It.Is<IReadOnlyList<SignupNotification>>(items =>
            items[0].AccessChanges![0].Granted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EnqueueFailuresRetryOnlyFailedStreamRecords()
    {
        var outbox = new Mock<INotificationOutbox>();
        outbox.Setup(x => x.EnqueueAsync(It.Is<IReadOnlyList<SignupNotification>>(items => items[0].Id.EndsWith("event-1")),
            It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Unavailable"));
        var logs = new List<string?>();
        var result = await new AccessChangeWorkflow(outbox.Object).EnqueueAsync(Stream(
            Record("event-1", "1", Image(), Image(Key("a", "1"))),
            Record("event-2", "2", Image(), Image(Key("b", "2")))), logs.Add);
        Assert.That(result.Failures, Is.EqualTo(new[] { new BatchFailure("1") }));
        Assert.That(logs, Is.EqualTo(new[] { "event-1" }));
        outbox.Verify(x => x.EnqueueAsync(It.Is<IReadOnlyList<SignupNotification>>(items => items[0].Id.EndsWith("event-2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MissingRecipientFailsInsteadOfDroppingAccessNotification()
    {
        var after = Image(Key("a", "1"));
        after.Remove("Email");
        var result = await new AccessChangeWorkflow(new Mock<INotificationOutbox>(MockBehavior.Strict).Object)
            .EnqueueAsync(Stream(Record("event", "1", Image(), after)), _ => { });
        Assert.That(result.Failures, Is.EqualTo(new[] { new BatchFailure("1") }));
    }

    [Test]
    public async Task AccessNotificationRoundTripsThroughOutboxAndEmailsUserInUkrainian()
    {
        var notification = new SignupNotification("access/user/event", "recipient@example.com", "email",
            Kind: "access-change", AccessChanges: [new("added-device", "-123", true), new("removed-device", "123", false)]);
        Dictionary<string, AttributeValue>? item = null;
        var db = new Mock<IAmazonDynamoDB>();
        db.Setup(x => x.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransactWriteItemsRequest, CancellationToken>((request, _) => item = request.TransactItems.Single().Put.Item)
            .ReturnsAsync(new TransactWriteItemsResponse());
        var outbox = new DynamoNotificationOutbox(db.Object, "outbox");
        await outbox.EnqueueAsync([notification], default);
        db.Setup(x => x.GetItemAsync(It.Is<GetItemRequest>(request => request.ConsistentRead), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new GetItemResponse { Item = item });
        var loaded = await outbox.LoadAsync(notification.Id, default);
        Assert.That(loaded!.Kind, Is.EqualTo("access-change"));
        Assert.That(loaded.AccessChanges, Is.EqualTo(notification.AccessChanges));
        SendEmailRequest? request = null;
        var ses = new Mock<IAmazonSimpleEmailService>();
        ses.Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequest, CancellationToken>((value, _) => request = value).ReturnsAsync(new SendEmailResponse());
        using var http = new HttpClient();
        await new NotificationSender(ses.Object, Mock.Of<IAmazonSimpleSystemsManagement>(), http,
            new("prod", "PingLight", "sender@example.com", "sbarsuk88@gmail.com", "38627946", "token", "https://pinglight.xyz/devices"))
            .SendAsync(loaded, default);
        Assert.That(request!.Destination.ToAddresses, Is.EqualTo(new[] { "recipient@example.com" }));
        Assert.That(request.Message.Subject.Data, Is.EqualTo("Зміна доступу до пристроїв"));
        Assert.That(request.Message.Body.Text.Data, Does.Contain("Вам надано доступ до таких пристроїв:")
            .And.Contain("Ваш доступ до таких пристроїв скасовано:")
            .And.Contain("Пристрій: added-device")
            .And.Contain("Пристрій: removed-device")
            .And.Contain("https://pinglight.xyz/devices"));
        foreach (var body in new[] { request.Message.Body.Text.Data, request.Message.Body.Html.Data })
            Assert.That(body, Does.Not.Contain("Середовище").And.Not.Contain("Проєкт")
                .And.Not.Contain("PingLight").And.Not.Contain("prod")
                .And.Not.Contain("чат:").And.Not.Contain("-123"));
        Assert.That(request.Message.Body.Html.Data, Does.Contain("href=\"https://pinglight.xyz/devices\"")
            .And.Contain("Перейти до моїх пристроїв"));
        Assert.That(request.Message.Body.Html.Charset, Is.EqualTo("UTF-8"));
        Assert.That(request.Message.Body.Text.Charset, Is.EqualTo("UTF-8"));
    }

    [TestCase("https://dev.pinglight.xyz/devices")]
    [TestCase("https://pinglight.xyz/devices")]
    public void AccessEmailButtonUsesConfiguredDevicesPageAndEscapesDeviceNames(string devicesUrl)
    {
        var notification = new SignupNotification("access", "user@example.com", "email", Kind: "access-change",
            AccessChanges: [new("<img src=x>", "hidden-chat-one", true), new("<img src=x>", "hidden-chat-two", true)]);
        var settings = new NotificationSettings("internal-stage", "internal-project", "sender@example.com",
            "admin@example.com", "chat", "token", devicesUrl);
        var html = NotificationSender.AccessEmailHtmlFor(notification, settings);
        Assert.That(html, Does.Contain($"href=\"{devicesUrl}\"").And.Contain("&lt;img src=x&gt;")
            .And.Not.Contain("<img src=x>").And.Not.Contain("hidden-chat")
            .And.Not.Contain("internal-stage").And.Not.Contain("internal-project"));
        var text = NotificationSender.EmailMessageFor(notification, settings);
        Assert.That(text.Split("<img src=x>").Length, Is.EqualTo(2), "Same device in multiple chats should appear once.");
        Assert.That(text, Does.Contain(devicesUrl).And.Not.Contain("hidden-chat"));
    }

    [Test]
    public async Task ExistingSignupOutboxRowsRemainReadable()
    {
        var db = new Mock<IAmazonDynamoDB>();
        db.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GetItemResponse
        {
            Item = new() { ["Id"] = new("signup/email"), ["Email"] = new("new@example.com"),
                ["Channel"] = new("email"), ["Delivered"] = new() { BOOL = false } }
        });
        var loaded = await new DynamoNotificationOutbox(db.Object, "outbox").LoadAsync("signup/email", default);
        Assert.That(loaded!.Kind, Is.EqualTo("signup"));
        Assert.That(loaded.AccessChanges, Is.Null);
    }

    [TestCase("CustomMessage_SignUp", "Підтвердження реєстрації")]
    [TestCase("CustomMessage_ResendCode", "Підтвердження реєстрації")]
    [TestCase("CustomMessage_ForgotPassword", "Відновлення пароля")]
    [TestCase("CustomMessage_UpdateUserAttribute", "Підтвердження електронної пошти")]
    [TestCase("CustomMessage_VerifyUserAttribute", "Підтвердження електронної пошти")]
    [TestCase("CustomMessage_Authentication", "Підтвердження входу")]
    [TestCase("CustomMessage_AdminCreateUser", "Запрошення до сервісу")]
    public void CognitoEmailsAreUkrainianAndPreserveCodePlaceholders(string source, string subject)
    {
        var message = JsonNode.Parse("""
            {"request":{"codeParameter":"{####}","usernameParameter":"{username}"},"response":{},"version":"1"}
            """)!.AsObject();
        message["triggerSource"] = source;
        Assert.That(new CognitoMessages().Customize(message), Is.SameAs(message));
        Assert.That(message["response"]!["emailSubject"]!.GetValue<string>(), Does.EndWith(subject));
        Assert.That(message["response"]!["emailSubject"]!.GetValue<string>(), Is.EqualTo(subject));
        var body = message["response"]!["emailMessage"]!.GetValue<string>();
        Assert.That(body, Does.Contain("{####}"));
        Assert.That(System.Net.WebUtility.HtmlDecode(body), Does.Contain("Вітаємо!"));
        if (source == "CustomMessage_AdminCreateUser") Assert.That(body, Does.Contain("{username}"));
        Assert.That(message["version"]!.GetValue<string>(), Is.EqualTo("1"));
    }
}
