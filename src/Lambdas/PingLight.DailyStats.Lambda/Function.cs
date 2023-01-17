using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Persistence;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.DailyStats.Lambda;

public class Function
{
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var isProd = input.IsProduction();
        var config = await ConfigBuilder.Build(isProd, context.Logger);
        var changesRepo = new ChangesRepository(context.Logger);
        var devicesRepo = new DeviceConfigRepository(isProd, context.Logger);

        var devices = await devicesRepo.GetConfigs();

        var from = DateTime.Today.AddDays(-1);
        var till = DateTime.Today;

        foreach (var device in devices)
        {
            context.Logger.LogInformation($"Querying for {device.DeviceId} from {from.ToString("O")} " +
                $"till {till.ToString("O")}");

            var changes = await changesRepo.GetChanges(device.DeviceId, from, till);
            var blackouts = BlackoutCalculator.Calculate(changes);

            // Post to TG
            var bot = new ChatBot(config.Token);
            var message = MessageBuilder.GetDailyStatsMessage(blackouts);

            var total = blackouts.Any() ? blackouts.Combine() : TimeSpan.Zero;
            var absentPercents = PercentCalculator.CalculateDailyPercents((int)total.TotalMinutes);
            var presentPercents = 100 - absentPercents;

            context.Logger.LogInformation($"Total minutes: {total.TotalMinutes}, " + 
                $"absentPercents = {absentPercents}, presentPercents = {presentPercents}.");

            var chart = ChartGenerator.Generate(presentPercents, absentPercents);
            await bot.PostImageBytes(chart, message, device.ChatId);

            context.Logger.LogInformation(message);
        }
    }
}
