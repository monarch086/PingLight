using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Model;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.DailyStats.Lambda;

public class Function
{
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var repo = new DynamoDbRepository(context.Logger);

        var from = DateTime.Today.AddDays(-1);
        var till = DateTime.Today;

        context.Logger.LogInformation($"Querying from {from.ToString("O")} till {till.ToString("O")}");

        var changes = await repo.GetChanges("12", from, till);
        var blackouts = CreateBlackoutTimespans(changes, context.Logger);

        var total = blackouts.Aggregate((a, b) => a.Add(b));

        context.Logger.LogInformation($"Found {blackouts.Count} blackouts for total {total.Hours} h {total.Minutes} m.");
    }

    private List<TimeSpan> CreateBlackoutTimespans(List<Change> changes, ILambdaLogger logger)
    {
        var blackouts = new List<TimeSpan>();

        for (int i = 0; i < changes.Count; i++)
        {
            if (i == 0 && changes[i].IsLight)
            {
                blackouts.Add(changes[i].ChangeDate - changes[i].ChangeDate.Date);
                logger.LogInformation($"Blackout: {changes[i].ChangeDate - changes[i].ChangeDate.Date}");
            }

            else if (i == changes.Count - 1 && !changes[i].IsLight)
            {
                var nextDay = changes[i].ChangeDate.Date.AddDays(1);
                blackouts.Add(nextDay - changes[i].ChangeDate);
                logger.LogInformation($"Blackout: {nextDay - changes[i].ChangeDate}");
            }

            else if (!changes[i].IsLight)
            {
                blackouts.Add(changes[i + 1].ChangeDate - changes[i].ChangeDate);
                logger.LogInformation($"Blackout: {changes[i + 1].ChangeDate - changes[i].ChangeDate}");
                //i++;
            }
        }

        return blackouts;
    }
}
