using Amazon.Lambda.Core;
using System.Text.Json.Nodes;
using System.Text.Json;
using PingLight.Core.Persistence;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Gather.Lambda;

public class Function
{
    /// <summary>
    /// A function that gets requests from detecting devices
    /// </summary>
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var inputData = input["queryStringParameters"].Deserialize<InputModel>();
        if (inputData == null) { return; }

        var pingsRepo = new PingsRepository(context.Logger);

        await pingsRepo.AddPing(inputData.Id);
    }
}