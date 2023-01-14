using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using PingLight.Core.Model;

namespace PingLight.Core
{
    internal static class DbMappings
    {
        public static Document ToDocument(this Change change)
        {
            var item = new Dictionary<string, AttributeValue>()
            {
                { "DeviceId", new AttributeValue { S = change.DeviceId } },
                { "ChangeDate", new AttributeValue { S = change.ChangeDate.ToString("O") } },
                { "IsLight", new AttributeValue { BOOL = change.IsLight }}
            };

            return Document.FromAttributeMap(item);
        }

        public static Change ToChange(this Document document)
        {
            return new Change
            {
                DeviceId = document["DeviceId"].AsString(),
                ChangeDate = DateTime.Parse(document["ChangeDate"].AsString()),
                IsLight = document["IsLight"].AsBoolean(),
            };
        }

        public static Document ToDocument(this PingInfo ping)
        {
            var attributes = new Dictionary<string, AttributeValue>()
              {
                  { "Id", new AttributeValue { S = ping.Id }},
                  { "LastPingDate", new AttributeValue { S = ping.LastPingDate.ToString("O") }},
              };

            return Document.FromAttributeMap(attributes);
        }

        public static PingInfo ToPingInfo(this Document document)
        {
            return new PingInfo
            {
                Id = document["Id"].AsString(),
                LastPingDate = DateTime.Parse(document["LastPingDate"])
            };
        }

        public static DeviceConfig.Config ToDeviceConfig(this Document document)
        {
            return new DeviceConfig.Config
            {
                DeviceId = document["DeviceId"].AsString(),
                ChatId = document["ChatId"].AsString(),
                Description = document["Description"].AsString()
            };
        }
    }
}
