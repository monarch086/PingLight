using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using PingLight.Core.HttpResponses;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");
        var customEnv = Environment.GetEnvironmentVariable("CUSTOM_ENV");

        var message = $"Hello from {customEnv} environment (stage: {stage})";

        context.Logger.LogInformation(message);

        return new SuccessResponse(message);
    }
}
