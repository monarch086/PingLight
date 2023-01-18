using Amazon.Lambda.Core;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    /// <summary>
    /// A simple function to test new functionality
    /// </summary>
    public void FunctionHandler(JsonObject input, ILambdaContext context)
    {
        // string env = Environment.GetEnvironmentVariable("environment");
        // context.Logger.LogInformation($"Env var environment = {env}.");

        // {"Environment": "prod"}
        context.Logger.LogInformation(input.ToJsonString());
    }
}
