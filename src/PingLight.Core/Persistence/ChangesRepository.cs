using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using PingLight.Core.Model;

namespace PingLight.Core.Persistence
{
    public class ChangesRepository
    {
        private static string changesTableName = "PingLight.Changes";

        private readonly AmazonDynamoDBClient client;
        private readonly Table changesTable;
        private readonly ILambdaLogger logger;

        public ChangesRepository(ILambdaLogger logger)
        {
            client = new AmazonDynamoDBClient();
            changesTable = Table.LoadTable(client, changesTableName);
            this.logger = logger;
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
            var changes = new List<Change>();

            var filter = new QueryFilter("DeviceId", QueryOperator.Equal, deviceId);
            filter.AddCondition("ChangeDate", QueryOperator.Between, from, till);

            var config = new QueryOperationConfig()
            {
                Limit = 10,
                Select = SelectValues.AllAttributes,
                ConsistentRead = true,
                Filter = filter
            };

            var queryResult = changesTable.Query(config);
            logger.LogInformation($"Query result count: {queryResult.Count}");

            do
            {
                var documents = await queryResult.GetNextSetAsync();
                foreach (var document in documents)
                {
                    changes.Add(document.ToChange());
                }
            } while (!queryResult.IsDone);

            return changes;
        }
    }
}
