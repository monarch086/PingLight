using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace PingLight.Core.HttpResponses
{
    public class BadRequestResponse : APIGatewayProxyResponse
    {
        public BadRequestResponse(string? error = null)
        {
            StatusCode = (int)HttpStatusCode.BadRequest;
            Headers = new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", "*" },
                { "Content-Type", "application/json" }
            };
            Body = error;
        }
    }
}
