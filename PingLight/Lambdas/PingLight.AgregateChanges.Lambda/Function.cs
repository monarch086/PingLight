using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.Model;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.AggregateChanges.Lambda;

public class Function
{
    private static string ChannelId = "38627946";

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var config = await ConfigBuilder.Build(false, context.Logger);
        var repo = new DynamoDbRepository(context.Logger);

        var pings = await repo.GetPings();

        foreach (var ping in pings)
        {
            var change = await repo.GetLatestChange(ping.Id);
            var changed = change == null || isChanged(ping.LastPingDate, change.IsLight);
            if (changed) await statusChanged(repo, config, ping, change);
        }

        context.Logger.LogInformation("Stream processing complete.");
    }

    private bool isChanged(DateTime lasPingDate, bool currentStatus)
    {
        var minuteAgo = TimeSpan.FromMinutes(1);
        var isLight = DateTime.UtcNow - lasPingDate <= minuteAgo;

        return isLight != currentStatus;
    }

    private async Task statusChanged(DynamoDbRepository repo, PingConfig config, PingInfo ping, Change? lastChange)
    {
        var isLight = lastChange != null ? !lastChange.IsLight : true;
        var timespan = lastChange != null ? DateTime.UtcNow - lastChange.ChangeDate : TimeSpan.Zero;

        // Add change to DB
        await repo.AddChange(new Change 
        {
            DeviceId = ping.Id,
            ChangeDate = DateTime.UtcNow,
            IsLight = isLight
        });

        // Post to TG
        var bot = new ChatBot(config.Token);
        var message = isLight ? bot.GetLightOnMessage(timespan) : bot.GetLightOffMessage(timespan);

        await bot.Post(message, ChannelId);
    }
}