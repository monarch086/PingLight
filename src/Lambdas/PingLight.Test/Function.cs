using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Persistence;
using System.Text;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var changesRepo = new ChangesRepository(context.Logger);
        var kyivZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kiev");

        var from = DateTime.Today.AddDays(-1).FromKyivTime();
        var till = DateTime.Today.FromKyivTime();

        context.Logger.LogInformation($"from: {from.Kind} - {from.ToString("o")}");
        context.Logger.LogInformation($"till: {till.Kind} - {till.ToString("o")}");
        context.Logger.LogInformation($"kyivZone: {kyivZone.StandardName} - {kyivZone.BaseUtcOffset}");

        var deviceId = "S4D-12";

        var changes = await changesRepo.GetChanges(deviceId, from, till);
        var logMessage = new StringBuilder($"Found {changes.Count} changes: \n\r");
        foreach (var change in changes)
        {
            logMessage.AppendLine($"{change.ChangeDate.ToString("o")}: {change.IsLight}");
        }

        context.Logger.LogInformation(logMessage.ToString());
    }
}
