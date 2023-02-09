using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Charts;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Model;
using PingLight.Core.Persistence;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.MonthlyStats.Lambda;

public class Function
{
    /// <summary>
    /// A function that calculates monthly blackout statistics
    /// </summary>
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var isProd = false;
        var config = await ConfigBuilder.Build(isProd, context.Logger);
        var bot = new ChatBot(config.Token);
        var changesRepo = new ChangesRepository(context.Logger);
        var devicesRepo = new DeviceConfigRepository(isProd, context.Logger);

        var devices = await devicesRepo.GetConfigs();

        var from = DateTime.Today.AddMonths(-1).FromKyivTime();
        var till = DateTime.Today.FromKyivTime();

        foreach (var device in devices)
        {
            var changes = await changesRepo.GetChanges(device.DeviceId, from, till);
            if (!changes.Any())
            {
                await AppendChangeBasedOnPrevious(changes, device.DeviceId, from, changesRepo);
            }

            var blackouts = BlackoutCalculator.CalculatePerDay(changes, from, till);
            var lights = blackouts.ToLightPerDay();

            // Post to TG
            var message = MessageBuilder.GetMonthlyStatsMessage(blackouts);

            var chart = BarChartGenerator.Generate(lights, from.ToKyivTime(), till.ToKyivTime());
            await bot.PostImageBytes(chart, message, device.ChatId);

            context.Logger.LogInformation(message);
        }
    }

    private async Task AppendChangeBasedOnPrevious(List<Change> changes, string deviceId, DateTime till, ChangesRepository repo)
    {
        var latestChange = await repo.GetLatestChange(deviceId, till);
        if (latestChange != null && !latestChange.IsLight)
        {
            changes.Add(new Change
            {
                DeviceId = deviceId,
                ChangeDate = till,
                IsLight = false
            });
        }
    }
}
