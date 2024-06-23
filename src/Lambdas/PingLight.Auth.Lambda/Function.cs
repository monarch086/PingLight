using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PingLight.Core.HttpResponses;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Auth.Lambda;

public class Function
{
    private readonly IConfiguration _configuration;

    public Function()
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");

        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{stage}.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }


    public async Task<APIGatewayProxyResponse> GenerateTokenAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var body = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Body);
        var username = body["username"];
        var password = body["password"];

        if (username == "testuser" && password == "password123")
        {
            var userId = "1";

            var accessToken = GenerateJwtToken(userId, "access");
            var refreshToken = GenerateJwtToken(userId, "refresh");

            var response = new Dictionary<string, string>
                {
                    { "access_token", accessToken },
                    { "refresh_token", refreshToken }
                };

            return new SuccessResponse(JsonConvert.SerializeObject(response));
        }
        else
        {
            return new UnauthorizedResponse("Invalid username or password");
        }
    }

    public async Task<APIGatewayProxyResponse> ValidateTokenAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var token = request.Headers["Authorization"];

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var claimsPrincipal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);


            var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                         claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenType = claimsPrincipal.FindFirst("type")?.Value;

            return new SuccessResponse($"Token is valid for userId: {userId}, tokenType: {tokenType}.");
        }
        catch (SecurityTokenException)
        {
            return new UnauthorizedResponse("Invalid token");
        }
        catch (Exception ex)
        {
            return new FailResponse($"Internal server error: {ex.Message}.");
        }
    }

    private string GenerateJwtToken(string userId, string tokenType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenExpiration = tokenType == "access"
            ? DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessExpirationMinutes"]))
            : DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshExpirationDays"]));

        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("type", tokenType),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Issuer"],
            claims: claims,
            expires: tokenExpiration,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
