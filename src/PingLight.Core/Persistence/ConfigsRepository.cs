using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2.Model;

namespace PingLight.Core.Persistence
{
    public class ConfigsRepository
    {
        private readonly AmazonDynamoDBClient client;
        private readonly Table table;
        private readonly ILambdaLogger logger;

        public ConfigsRepository(string stage, ILambdaLogger logger)
        {
            var tableName = $"PingLight.{stage}.Configurations";
            client = new AmazonDynamoDBClient();
            table = Table.LoadTable(client, tableName);
            this.logger = logger;
        }

        public async Task AddOrUpdateAsync(string groupKey, string configKey, string value)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                { "GroupKey", new AttributeValue { S = groupKey } },
                { "ConfigKey", new AttributeValue { S = configKey } },
                { "ConfigValue", new AttributeValue { S = value } }
            };

            await table.PutItemAsync(Document.FromAttributeMap(item));
        }

        public async Task<string?> GetAsync(string groupKey, string configKey)
        {
            var filter = new QueryFilter("GroupKey", QueryOperator.Equal, groupKey);
            filter.AddCondition("ConfigKey", QueryOperator.Equal, configKey);

            var config = new QueryOperationConfig()
            {
                Limit = 1,
                Select = SelectValues.AllAttributes,
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = filter
            };

            var queryResult = table.Query(config);

            var documents = await queryResult.GetNextSetAsync();

            if (documents.Count > 0)
            {
                documents[0]["ConfigValue"].AsString();
            }

            return null;
        }
    }
}
