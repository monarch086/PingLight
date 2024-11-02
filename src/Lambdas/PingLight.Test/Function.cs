using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using PingLight.Core.HttpResponses;
using System.Text.Json;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    private readonly IAmazonDynamoDB DynamoDbClient = new AmazonDynamoDBClient();
    private static readonly string InitialStage = "initial";

    public async Task<APIGatewayProxyResponse> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var stage = Environment.GetEnvironmentVariable("STAGE") ?? InitialStage;
        var customEnv = Environment.GetEnvironmentVariable("CUSTOM_ENV") ?? InitialStage;

        var inputData = input["queryStringParameters"].Deserialize<InputModel>();

        var message = $"Hello from {customEnv} environment (stage: {stage})";

        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = stage == InitialStage ? $"PingLight.Changes" : $"PingLight.{stage}.Changes"
            };

            var scanResponse = await DynamoDbClient.ScanAsync(scanRequest);
            message += $"\nFound {scanResponse.Count} records.";

            //await CopyTableAsync(context);

            //var eventTester = new CalendarEventsTester();

            //message += await eventTester.LoadCalendarEventsAsync(context, stage, inputData);

            return new SuccessResponse(message);
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.ToString());
        }
    }
}
