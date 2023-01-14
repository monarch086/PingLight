using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using PingLight.Core.Model;

namespace PingLight.Core.Persistence
{
    public class PingsRepository
    {
        private static string pingTableName = "PingLight.Status";

        private readonly AmazonDynamoDBClient client;
        private readonly Table pingTable;
        private readonly ILambdaLogger logger;

        public PingsRepository(ILambdaLogger logger)
        {
            client = new AmazonDynamoDBClient();
            pingTable = Table.LoadTable(client, pingTableName);
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
    }
}
