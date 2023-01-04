using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using System.Text.Json.Nodes;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Gather.Lambda;

public class Function
{
    private static string tableName = "PingLight.Status";

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var inputData = input["queryStringParameters"].Deserialize<InputModel>();

        var client = new AmazonDynamoDBClient();
        var table = Table.LoadTable(client, tableName);

        var item = createDynamoDBItem(inputData.Id);

        var document = Document.FromAttributeMap(item);

        await table.PutItemAsync(document);
    }

    private Dictionary<string, AttributeValue> createDynamoDBItem(string id)
    {
        return new Dictionary<string, AttributeValue>()
          {
              { "Id", new AttributeValue { S = id }},
              { "LastPingDate", new AttributeValue { S = DateTime.UtcNow.ToString("O") }},
          };
    }
}