using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2.Model;
using PingLight.Core.Model;

namespace PingLight.AggregateChanges.Lambda
{
    internal class DynamoDbRepository
    {
        private static string pingTableName = "PingLight.Status";
        private static string changesTableName = "PingLight.Changes";

        private readonly AmazonDynamoDBClient client;
        private readonly Table pingTable;
        private readonly Table changesTable;
        private readonly ILambdaLogger logger;

        public DynamoDbRepository(ILambdaLogger logger)
        {
            client = new AmazonDynamoDBClient();
            pingTable = Table.LoadTable(client, pingTableName);
            changesTable = Table.LoadTable(client, changesTableName);
            this.logger = logger;
        }

        public async Task<List<PingInfo>> GetPings()
        {
            var pings = new List<PingInfo>();

            var scanFilter = new ScanFilter();
            var scanResult = pingTable.Scan(scanFilter);

            logger.LogInformation($"Scan result count: {scanResult.Count}");

            do
            {
                var documents = await scanResult.GetNextSetAsync();
                foreach (var document in documents)
                {
                    var deviceId = document["Id"];
                    var lastPingDate = DateTime.Parse(document["LastPingDate"]);

                    pings.Add(new PingInfo { Id = deviceId, LastPingDate = lastPingDate });
                }
            } while (!scanResult.IsDone);

            return pings;
        }

        public async Task<Change?> GetLatestChange(string deviceId)
        {
            var config = new QueryOperationConfig()
            {
                Limit = 1,
                Select = SelectValues.AllAttributes,
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter("DeviceId", QueryOperator.Equal, deviceId)
            };

            var queryResult = changesTable.Query(config);
            logger.LogInformation($"Query result count: {queryResult.Count}");

            var records = await queryResult.GetNextSetAsync();

            if (records.Count > 0)
            {
                logger.LogInformation($"Found change: {records[0]["DeviceId"]} - {records[0]["ChangeDate"]}");

                return new Change
                {
                    DeviceId = records[0]["DeviceId"].AsString(),
                    ChangeDate = DateTime.Parse(records[0]["ChangeDate"].AsString()),
                    IsLight = records[0]["IsLight"].AsBoolean(),
                };
            }

            return null;
        }

        public async Task AddChange(Change change)
        {
            var item = new Dictionary<string, AttributeValue>()
            {
                { "DeviceId", new AttributeValue { S = change.DeviceId } },
                { "ChangeDate", new AttributeValue { S = change.ChangeDate.ToString("O") } },
                { "IsLight", new AttributeValue { BOOL = change.IsLight }}
            };

            var document = Document.FromAttributeMap(item);

            await changesTable.PutItemAsync(document);
        }
    }
}
