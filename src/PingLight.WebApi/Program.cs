using Amazon.DynamoDBv2;
using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authorization;
using PingLight.WebApi.Authorization;
using PingLight.WebApi.Devices;
using PingLight.WebApi.ServiceExtensions;
using PingLight.WebApi.Users;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(SystemAdminAttribute.PolicyName, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new SystemAdminRequirement()));
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
    .WithMethods("GET", "PUT", "DELETE").WithHeaders("Authorization", "Content-Type")));
builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ => new AmazonCognitoIdentityProviderClient());
builder.Services.AddSingleton<IDeviceStore, DynamoDeviceStore>();
builder.Services.AddSingleton<IUserStore, DynamoUserStore>();
builder.Services.AddSingleton<IUserDirectory, CognitoUserDirectory>();
builder.Services.AddSingleton<IAuthorizationHandler, SystemAdminAuthorizationHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.MapControllers();
app.Run();

public partial class Program { }
