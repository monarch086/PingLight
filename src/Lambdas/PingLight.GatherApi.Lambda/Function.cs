using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Text.Json.Nodes;
using PingLight.Core.Persistence;
using PingLight.Core.HttpResponses;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.GatherApi.Lambda;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        try
        {
            var inputData = input["queryStringParameters"].Deserialize<InputModel>();
            if (inputData == null) { return new BadRequestResponse("Failed to deserialize input model."); }

            var stage = Environment.GetEnvironmentVariable("STAGE");
            var pingsRepository = new PingsRepository(stage, context.Logger);

            await pingsRepository.AddPing(inputData.Id);

            return new SuccessResponse("Ping saved successfully.");
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.ToString());
        }
    }
}
