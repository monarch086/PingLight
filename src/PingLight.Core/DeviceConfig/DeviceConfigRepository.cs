using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using PingLight.Core.Persistence;

namespace PingLight.Core.DeviceConfig
{
    public class DeviceConfigRepository
    {
        private const string TABLE_NAME = "PingLight.DeviceConfigs";

        private readonly AmazonDynamoDBClient client;
        private readonly Table configTable;
        private readonly ILambdaLogger logger;

        public DeviceConfigRepository(ILambdaLogger logger)
        {
            client = new AmazonDynamoDBClient();
            configTable = Table.LoadTable(client, TABLE_NAME);
            this.logger = logger;
        }

        public async Task<List<Config>> GetConfigs()
        {
            var configs = new List<Config>();

            var scanFilter = new ScanFilter();
            var scanResult = configTable.Scan(scanFilter);

            logger.LogInformation($"[DeviceConfigRepo] Scan result count: {scanResult.Count}");

            do
            {
                var documents = await scanResult.GetNextSetAsync();
                foreach (var document in documents)
                {
                    configs.Add(document.ToDeviceConfig());
                }
            } while (!scanResult.IsDone);

            return configs;
        }
    }
}
