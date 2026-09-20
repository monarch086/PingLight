using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using PingLight.WebApi.Devices;

namespace PingLight.WebApi.Tests;

public class ManagementApiTests
{
    private const string Issuer = "https://cognito-idp.eu-central-1.amazonaws.com/test";
    private const string ClientId = "test-client";
    private RSA rsa = null!;
    private RsaSecurityKey key = null!;
    private Mock<IAmazonDynamoDB> db = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        rsa = RSA.Create(2048);
        key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        db = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Cognito:Authority", Issuer);
            builder.UseSetting("Cognito:ClientId", ClientId);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cognito:Authority"] = Issuer,
                ["Cognito:ClientId"] = ClientId,
                ["Devices:TableName"] = "PingLight.test.DeviceConfigs",
                ["AllowedOrigins:0"] = "https://frontend.test"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAmazonDynamoDB>();
                services.AddSingleton(db.Object);
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                    metadata.SigningKeys.Add(key);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
                });
            });
        });
        client = factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        factory.Dispose();
        rsa.Dispose();
    }

    private void Authenticate(string tokenUse = "access", string appClient = ClientId,
        string issuer = Issuer, bool expired = false, SecurityKey? signingKey = null)
    {
        var token = new JwtSecurityToken(issuer, null,
            [new Claim("sub", "owner-a"), new Claim("client_id", appClient), new Claim("token_use", tokenUse)],
            DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(10),
            new SigningCredentials(signingKey ?? key, SecurityAlgorithms.RsaSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    [Test]
    public async Task AnonymousCannotReadOrWrite()
    {
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PutAsJsonAsync("/devices/a/destinations/chat-a/settings", new DeviceSettings())).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        db.VerifyNoOtherCalls();
    }

    [TestCase("id", ClientId, Issuer, false)]
    [TestCase("access", "another-client", Issuer, false)]
    [TestCase("access", ClientId, "https://other-issuer.test", false)]
    [TestCase("access", ClientId, Issuer, true)]
    public async Task InvalidTokenCannotRead(string tokenUse, string appClient, string issuer, bool expired)
    {
        Authenticate(tokenUse, appClient, issuer, expired);
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        db.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ForgedSignatureCannotRead()
    {
        using var otherRsa = RSA.Create(2048);
        Authenticate(signingKey: new RsaSecurityKey(otherRsa) { KeyId = "test-key" });
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        db.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ListUsesVerifiedOwnerAndReturnsDestinationWithoutOwnerMetadata()
    {
        Authenticate();
        db.Setup(x => x.ScanAsync(It.Is<ScanRequest>(r =>
            r.TableName == "PingLight.test.DeviceConfigs" &&
            r.FilterExpression == "#owner = :owner" &&
            r.ExpressionAttributeNames["#owner"] == "OwnerId" &&
            r.ExpressionAttributeValues[":owner"].S == "owner-a" && r.Limit == 100),
            It.IsAny<CancellationToken>())).ReturnsAsync(new ScanResponse
            {
                Items = [new() { ["DeviceId"] = new("a"), ["ChatId"] = new("private-chat"), ["OwnerId"] = new("owner-a") }]
            });
        var response = await client.GetAsync("/devices?ownerId=someone-else");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Contain("owner-a"));
        var page = await response.Content.ReadFromJsonAsync<DevicePage>();
        Assert.That(page!.Items.Single().DeviceId, Is.EqualTo("a"));
        Assert.That(page.Items.Single().ChatId, Is.EqualTo("private-chat"));
        Assert.That(response.Headers.CacheControl!.NoStore, Is.True);
    }

    [Test]
    public async Task SettingsUpdateChecksOwnershipAtomicallyAndPreservesOtherFields()
    {
        Authenticate();
        db.Setup(x => x.UpdateItemAsync(It.Is<UpdateItemRequest>(r =>
            r.Key["DeviceId"].S == "a" && r.Key["ChatId"].S == "chat-a" &&
            r.ConditionExpression == "attribute_exists(DeviceId) AND #owner = :owner" &&
            r.ExpressionAttributeValues[":owner"].S == "owner-a" &&
            r.ExpressionAttributeValues[":description"].S == "Home" &&
            !r.UpdateExpression.Contains("OwnerId") && !r.UpdateExpression.Contains("ChatId") &&
            !r.UpdateExpression.Contains("IsActive")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());
        var response = await client.PutAsJsonAsync("/devices/a/destinations/chat-a/settings",
            new { description = " Home ", notificationDelaySec = 30, ownerId = "other", chatId = "other", isActive = true });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        db.VerifyAll();
    }

    [Test]
    public async Task ForeignOrMissingDeviceReturnsNotFound()
    {
        Authenticate();
        db.Setup(x => x.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("Does not belong to owner."));
        Assert.That((await client.PutAsJsonAsync("/devices/foreign/destinations/chat-a/settings", new DeviceSettings())).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase(-1)]
    [TestCase(86401)]
    public async Task InvalidDelayDoesNotWrite(int delay)
    {
        Authenticate();
        Assert.That((await client.PutAsJsonAsync("/devices/a/destinations/chat-a/settings", new DeviceSettings { NotificationDelaySec = delay })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        db.VerifyNoOtherCalls();
    }

    [Test]
    public async Task NullDescriptionDoesNotWrite()
    {
        Authenticate();
        Assert.That((await client.PutAsJsonAsync("/devices/a/destinations/chat-a/settings", new { description = (string?)null })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        db.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ScanContinuesAcrossEmptyPagesWithoutLeakingCursorKeys()
    {
        Authenticate();
        var cursor = new Dictionary<string, AttributeValue> { ["DeviceId"] = new("foreign-device"), ["ChatId"] = new("foreign-chat") };
        db.SetupSequence(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResponse { Items = [], LastEvaluatedKey = cursor })
            .ReturnsAsync(new ScanResponse { Items = [new() { ["DeviceId"] = new("a"), ["ChatId"] = new("chat-a") }] });
        var response = await client.GetAsync("/devices");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Contain("foreign-device").And.Not.Contain("foreign-chat"));
        db.Verify(x => x.ScanAsync(It.Is<ScanRequest>(r => r.ExclusiveStartKey == cursor), It.IsAny<CancellationToken>()), Times.Once);
    }
}
