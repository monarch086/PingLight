using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace PingLight.Core.HttpResponses
{
    public class FailResponse : APIGatewayProxyResponse
    {
        public FailResponse(string error)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError;
            Headers = new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", "*" },
                { "Content-Type", "application/json" }
            };
            Body = error;
        }
    }
}
