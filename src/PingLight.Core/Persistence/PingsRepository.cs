using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using PingLight.Core.Model;

namespace PingLight.Core.Persistence
{
    public class PingsRepository
    {
        private readonly AmazonDynamoDBClient client;
        private readonly Table pingTable;
        private readonly ILambdaLogger logger;

        public PingsRepository(string stage, ILambdaLogger logger)
        {
            var tableName = $"PingLight.{stage}.Status";
            client = new AmazonDynamoDBClient();
            pingTable = Table.LoadTable(client, tableName);
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

        public async Task<PingInfo?> GetPing(string deviceId)
        {
            var config = new QueryOperationConfig()
            {
                Limit = 1,
                Select = SelectValues.AllAttributes,
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter("Id", QueryOperator.Equal, deviceId)
            };

            var queryResult = pingTable.Query(config);

            var documents = await queryResult.GetNextSetAsync();

            if (documents.Count > 0)
            {
                return documents[0].ToPingInfo();
            }

            return null;
        }
    }
}
