using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PingLight.WebApi.ServiceExtensions;

public static class AuthExtensions
{
    public static void AddCognitoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Cognito:Authority"];
        var clientId = configuration["Cognito:ClientId"];
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != "https" || string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Configure Cognito:Authority and Cognito:ClientId before starting the API.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = issuer;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                // Cognito access tokens identify the app in client_id, not aud.
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.FindFirst("token_use")?.Value != "access" ||
                        context.Principal.FindFirst("client_id")?.Value != clientId ||
                        string.IsNullOrWhiteSpace(context.Principal.FindFirst("sub")?.Value))
                        context.Fail("A valid Cognito access token for this application is required.");
                    return Task.CompletedTask;
                }
            };
        });
    }
}
