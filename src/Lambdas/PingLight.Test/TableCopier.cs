using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

namespace PingLight.Test
{
    internal class TableCopier
    {
        private readonly IAmazonDynamoDB DynamoDbClient = new AmazonDynamoDBClient();
        private readonly string SourceTableName = "PingLight.Changes";
        private readonly string DestinationTableName = "PingLight.prod.Changes";

        public async Task CopyTableAsync(ILambdaContext context)
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
}
