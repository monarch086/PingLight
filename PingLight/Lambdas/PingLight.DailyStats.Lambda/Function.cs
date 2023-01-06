using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.Model;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.DailyStats.Lambda;

public class Function
{
    private const string CHANNEL_ID = "38627946";

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var config = await ConfigBuilder.Build(false, context.Logger);
        var repo = new DynamoDbRepository(context.Logger);

        var from = DateTime.Today.AddDays(-1);
        var till = DateTime.Today;

        context.Logger.LogInformation($"Querying from {from.ToString("O")} till {till.ToString("O")}");

        var changes = await repo.GetChanges("12", from, till);
        var blackouts = CreateBlackoutTimespans(changes, context.Logger);

        // Post to TG
        var bot = new ChatBot(config.Token);
        var message = MessageBuilder.GetDailyStatsMessage(blackouts);

        //var chart = ChartGenerator.GenerateUrl();
        //await bot.Post(message + "\n" + chart, CHANNEL_ID);

        var total = blackouts.combineTimespans();
        var absentPercents = PercentCalculator.CalculateDailyPercents((int)total.TotalMinutes);
        var presentPercents = 100 - absentPercents;

        var chart = ChartGenerator.Generate(presentPercents, absentPercents);
        await bot.PostImageBytes(chart, message, CHANNEL_ID);

        context.Logger.LogInformation(message);
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
