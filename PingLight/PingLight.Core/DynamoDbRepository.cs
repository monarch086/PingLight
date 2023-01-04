using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using PingLight.Core.Model;

namespace PingLight.Core
{
    public class DynamoDbRepository
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

        public async Task AddPing(string deviceId)
        {
            var item = new PingInfo { Id = deviceId, LastPingDate = DateTime.UtcNow };

            await pingTable.PutItemAsync(item.ToDocument());
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
                    pings.Add(document.ToPingInfo());
                }
            } while (!scanResult.IsDone);

            return pings;
        }

        public async Task AddChange(Change change)
        {
            await changesTable.PutItemAsync(change.ToDocument());
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

            var documents = await queryResult.GetNextSetAsync();

            if (documents.Count > 0)
            {
                logger.LogInformation($"Found change: {documents[0]["DeviceId"]} - {documents[0]["ChangeDate"]}");

                return documents[0].ToChange();
            }

            return null;
        }

        public async Task<List<Change>> GetChanges(string deviceId, DateTime from, DateTime till)
        {
            return new List<Change>();
        }
    }
}
