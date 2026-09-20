using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using PingLight.WebApi.Devices;
using PingLight.WebApi.Users;

namespace PingLight.WebApi.Tests;

public class ManagementApiTests
{
    private const string Issuer = "https://cognito-idp.eu-central-1.amazonaws.com/test";
    private const string ClientId = "test-client";
    private RSA rsa = null!;
    private RsaSecurityKey key = null!;
    private Mock<IUserStore> users = null!;
    private Mock<IUserDirectory> directory = null!;
    private Mock<IDeviceStore> devices = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        rsa = RSA.Create(2048);
        key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        users = new(MockBehavior.Strict);
        directory = new(MockBehavior.Strict);
        devices = new(MockBehavior.Strict);
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting("Cognito:Authority", Issuer);
            builder.UseSetting("Cognito:ClientId", ClientId);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cognito:Authority"] = Issuer,
                ["Cognito:ClientId"] = ClientId,
                ["Devices:TableName"] = "devices",
                ["Users:TableName"] = "users",
                ["AllowedOrigins:0"] = "https://frontend.test"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserStore>();
                services.RemoveAll<IUserDirectory>();
                services.RemoveAll<IDeviceStore>();
                services.AddSingleton(users.Object);
                services.AddSingleton(directory.Object);
                services.AddSingleton(devices.Object);
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
        client?.Dispose();
        factory?.Dispose();
        rsa.Dispose();
    }

    private void Authenticate(string tokenUse = "access", string appClient = ClientId,
        string issuer = Issuer, bool expired = false, SecurityKey? signingKey = null)
    {
        var token = new JwtSecurityToken(issuer, null,
            [new Claim("sub", "user-a"), new Claim("email", "user@example.com"), new Claim("client_id", appClient), new Claim("token_use", tokenUse)],
            DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(10),
            new SigningCredentials(signingKey ?? key, SecurityAlgorithms.RsaSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    private void Current(bool admin = false, params string[] grants)
    {
        var user = new UserAccess("user-a", "user@example.com", admin, new HashSet<string>(grants));
        users.Setup(x => x.GetOrCreateAsync("user-a", "user@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
    }

    [Test]
    public async Task AnonymousCannotUseManagementApi()
    {
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.GetAsync("/users")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        users.VerifyNoOtherCalls();
        devices.VerifyNoOtherCalls();
    }

    [TestCase("id", ClientId, Issuer, false)]
    [TestCase("access", "another-client", Issuer, false)]
    [TestCase("access", ClientId, "https://other-issuer.test", false)]
    [TestCase("access", ClientId, Issuer, true)]
    public async Task InvalidTokenCannotRead(string tokenUse, string appClient, string issuer, bool expired)
    {
        Authenticate(tokenUse, appClient, issuer, expired);
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        users.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GeneralUserOnlyListsGrantedDevices()
    {
        Authenticate();
        var grant = UserGrantKey.Encode("device-a", "chat-a");
        Current(grants: grant);
        devices.Setup(x => x.ListAsync(It.Is<IReadOnlySet<string>>(set => set.SetEquals(new[] { grant })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DevicePage([]));

        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        devices.VerifyAll();
    }

    [Test]
    public async Task AdminListsAllDevices()
    {
        Authenticate();
        Current(admin: true);
        devices.Setup(x => x.ListAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(new DevicePage([]));
        Assert.That((await client.GetAsync("/devices")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        devices.VerifyAll();
    }

    [Test]
    public async Task GeneralUserCannotListUsersOrGrantDevices()
    {
        Authenticate();
        Current();
        Assert.That((await client.GetAsync("/users")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await client.PutAsync("/users/other/devices/a/destinations/chat-a", null)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        devices.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AdminCanListUsersAndGrantExistingDevice()
    {
        Authenticate();
        Current(admin: true);
        directory.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DirectoryUser("user-a", "user@example.com")]);
        devices.Setup(x => x.ExistsAsync("a", "chat-a", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        directory.Setup(x => x.FindAsync("other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryUser("other", "other@example.com"));
        users.Setup(x => x.GetOrCreateAsync("other", "other@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAccess("other", "other@example.com", false, new HashSet<string>()));
        users.Setup(x => x.SetGrantAsync("other", "a", "chat-a", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.That((await client.GetAsync("/users")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.PutAsync("/users/other/devices/a/destinations/chat-a", null)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task GeneralUserCanUpdateGrantedDeviceButNotAnotherDevice()
    {
        Authenticate();
        Current(grants: UserGrantKey.Encode("a", "chat-a"));
        devices.Setup(x => x.UpdateAsync("a", "chat-a", It.Is<DeviceSettings>(s => s.Description == " Home "), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var allowed = await client.PutAsJsonAsync("/devices/a/destinations/chat-a/settings", new DeviceSettings { Description = " Home " });
        var denied = await client.PutAsJsonAsync("/devices/b/destinations/chat-b/settings", new DeviceSettings());
        Assert.That(allowed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        devices.VerifyAll();
    }

    [Test]
    public async Task GeneralUserCanToggleNotificationsForGrantedDeviceButNotAnotherDevice()
    {
        Authenticate();
        Current(grants: UserGrantKey.Encode("a", "chat-a"));
        devices.Setup(x => x.SetActiveAsync("a", "chat-a", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var allowed = await client.PutAsJsonAsync("/devices/a/destinations/chat-a/notifications", new NotificationState(false));
        var denied = await client.PutAsJsonAsync("/devices/b/destinations/chat-b/notifications", new NotificationState(true));
        Assert.That(allowed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        devices.VerifyAll();
    }
}

public class DynamoDeviceStoreTests
{
    [Test]
    public async Task NotificationToggleOnlyUpdatesActiveFlag()
    {
        var db = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
        db.Setup(x => x.UpdateItemAsync(It.Is<UpdateItemRequest>(request =>
                request.UpdateExpression == "SET IsActive = :isActive" &&
                request.ConditionExpression == "attribute_exists(DeviceId)" &&
                request.ExpressionAttributeValues.Count == 1 &&
                request.ExpressionAttributeValues[":isActive"].BOOL == false), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());
        var store = new DynamoDeviceStore(db.Object, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Devices:TableName"] = "devices" }).Build());

        Assert.That(await store.SetActiveAsync("a", "chat-a", false, CancellationToken.None), Is.True);
        db.VerifyAll();
    }
}

public class DynamoUserStoreTests
{
    [Test]
    public async Task RegistrationPreservesManuallyAssignedAdminFlag()
    {
        var db = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
        db.Setup(x => x.UpdateItemAsync(It.Is<UpdateItemRequest>(request =>
                request.UpdateExpression.Contains("IsSystemAdmin = if_not_exists(IsSystemAdmin, :notAdmin)") &&
                request.ExpressionAttributeValues[":notAdmin"].BOOL == false &&
                request.ReturnValues == ReturnValue.ALL_NEW), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse
            {
                Attributes = new()
                {
                    ["UserId"] = new("admin-a"), ["Email"] = new("admin@example.com"),
                    ["IsSystemAdmin"] = new() { BOOL = true }
                }
            });
        var store = new DynamoUserStore(db.Object, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Users:TableName"] = "users" }).Build());

        var user = await store.GetOrCreateAsync("admin-a", "admin@example.com", CancellationToken.None);
        Assert.That(user.IsSystemAdmin, Is.True);
        db.VerifyAll();
    }
}

public class CognitoUserDirectoryTests
{
    [Test]
    public async Task ListsEveryCognitoPageAndUsesStableSubjectIds()
    {
        var cognito = new Mock<IAmazonCognitoIdentityProvider>(MockBehavior.Strict);
        cognito.SetupSequence(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListUsersResponse
            {
                PaginationToken = "next",
                Users = [CognitoUser("internal-b", "sub-b", "b@example.com")]
            })
            .ReturnsAsync(new ListUsersResponse
            {
                Users = [CognitoUser("internal-a", "sub-a", "a@example.com")]
            });
        var directory = new CognitoUserDirectory(cognito.Object, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Cognito:UserPoolId"] = "pool" }).Build());

        var users = await directory.ListAsync(CancellationToken.None);
        Assert.That(users.Select(user => user.UserId), Is.EqualTo(new[] { "sub-a", "sub-b" }));
        cognito.Verify(x => x.ListUsersAsync(It.Is<ListUsersRequest>(request => request.PaginationToken == "next"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserType CognitoUser(string username, string subject, string email) => new()
    {
        Username = username,
        Attributes = [new() { Name = "sub", Value = subject }, new() { Name = "email", Value = email }]
    };
}
