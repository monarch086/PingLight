using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using PingLight.Core.HttpResponses;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    private static readonly IAmazonDynamoDB DynamoDbClient = new AmazonDynamoDBClient();
    private static readonly string SourceTableName = "PingLight.Changes";
    private static readonly string DestinationTableName = "PingLight.prod.Changes";

    public async Task<APIGatewayProxyResponse> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");
        var customEnv = Environment.GetEnvironmentVariable("CUSTOM_ENV");

        //var message = $"Hello from {customEnv} environment (stage: {stage})";
        //context.Logger.LogInformation(message);

        try
        {
            await CopyTableAsync(context);

            return new SuccessResponse("Data copy completed successfully.");
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.ToString());
        }
    }

    public static async Task CopyTableAsync(ILambdaContext context)
    {
        Console.WriteLine($"Copying data from {SourceTableName} to {DestinationTableName}...");

        var scanRequest = new ScanRequest
        {
            TableName = SourceTableName
        };

        var scanResponse = await DynamoDbClient.ScanAsync(scanRequest);
        context.Logger.LogInformation($"Found {scanResponse.Count} records.");

        var counter = 0;

        foreach (var item in scanResponse.Items)
        {
            if (counter % 100 == 0)
                context.Logger.LogInformation($"Processed: {counter} records.");

            var putItemRequest = new PutItemRequest
            {
                TableName = DestinationTableName,
                Item = item
            };

            await DynamoDbClient.PutItemAsync(putItemRequest);

            counter++;
        }

        context.Logger.LogInformation("Data copy completed successfully.");
    }
}
