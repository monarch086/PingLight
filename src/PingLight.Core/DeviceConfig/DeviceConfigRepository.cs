using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using PingLight.Core.Persistence;

namespace PingLight.Core.DeviceConfig
{
    public class DeviceConfigRepository
    {
        private const string TABLE_NAME = "PingLight.DeviceConfigs";
        private const string TEST_TABLE_NAME = "PingLight.DeviceConfigs.Test";

        private readonly AmazonDynamoDBClient client;
        private readonly Table configTable;
        private readonly ILambdaLogger logger;

        public DeviceConfigRepository(bool isProd, ILambdaLogger logger)
        {
            client = new AmazonDynamoDBClient();
            configTable = Table.LoadTable(client, isProd ? TABLE_NAME : TEST_TABLE_NAME);
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
                    try
                    {
                        configs.Add(document.ToDeviceConfig());
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }
            } while (!scanResult.IsDone);

            return configs;
        }
    }
}
