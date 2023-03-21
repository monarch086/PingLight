using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Persistence;
using System.Text;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Test;

public class Function
{
    /// <summary>
    /// A simple function to test new functionality
    /// </summary>
    ///
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var changesRepo = new ChangesRepository(context.Logger);
        var kyivZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kiev");

        var from = DateTime.Today.AddDays(-1).FromKyivTime();
        var till = DateTime.Today.FromKyivTime();

        //"2023-01-21T22:00:00.0000000+00:00"
        //var till = DateTime.Parse("2023-01-22T00:00:00.0000000+02:00");

        //DateTime.SpecifyKind(from, DateTimeKind.Local);
        //DateTime.SpecifyKind(till, DateTimeKind.Local);

        context.Logger.LogInformation($"from: {from.Kind} - {from.ToString("o")}");
        context.Logger.LogInformation($"till: {till.Kind} - {till.ToString("o")}");
        context.Logger.LogInformation($"kyivZone: {kyivZone.StandardName} - {kyivZone.BaseUtcOffset}");
        //context.Logger.LogInformation($"localZone: {TimeZoneInfo.Local.StandardName} - {TimeZoneInfo.Local.BaseUtcOffset}");

        //var localFrom = TimeZoneInfo.ConvertTime(from, kyivZone);
        //var localTill = TimeZoneInfo.ConvertTime(till, kyivZone);

        //context.Logger.LogInformation($"from: {from.Kind} - {from.ToString("o")}");
        //context.Logger.LogInformation($"from.ToLocalTime(): {from.ToLocalTime().Kind} - {from.ToLocalTime().ToString("o")}");
        //context.Logger.LogInformation($"from.ToUniversalTime(): {from.ToUniversalTime().Kind} - {from.ToUniversalTime().ToString("o")}");

        //var convertedFrom = TimeZoneInfo.ConvertTimeToUtc(from, kyivZone);
        //var convertedTill = TimeZoneInfo.ConvertTimeToUtc(till, kyivZone);

        //DateTime.SpecifyKind(convertedFrom, DateTimeKind.Local);
        //DateTime.SpecifyKind(convertedTill, DateTimeKind.Local);

        //convertedFrom = TimeZoneInfo.ConvertTimeToUtc(convertedFrom, kyivZone);
        //convertedTill = TimeZoneInfo.ConvertTimeToUtc(convertedTill, kyivZone);

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
