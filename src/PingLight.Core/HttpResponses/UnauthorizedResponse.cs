using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace PingLight.Core.HttpResponses
{
    public class UnauthorizedResponse : APIGatewayProxyResponse
    {
        public UnauthorizedResponse(string body)
        {
            StatusCode = (int)HttpStatusCode.Unauthorized;
            Headers = new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", "*" },
                { "Content-Type", "application/json" }
            };
            Body = body;
        }
    }
}
