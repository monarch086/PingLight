using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Model;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.AggregateChanges.Lambda;

public class Function
{
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var config = await ConfigBuilder.Build(false, context.Logger);
        var repo = new DynamoDbRepository(context.Logger);
        var deviceRepo = new DeviceConfigRepo(context.Logger);

        var devices = await deviceRepo.GetConfigs();
        var pings = await repo.GetPings();

        foreach (var ping in pings)
        {
            var change = await repo.GetLatestChange(ping.Id);
            var changed = change == null || isChanged(ping.LastPingDate, change.IsLight);
            var device = devices.FirstOrDefault(d => d.DeviceId == ping.Id);
            if (changed) await statusChanged(repo, config, ping, change, device);
        }

        context.Logger.LogInformation("Stream processing complete.");
    }

    private bool isChanged(DateTime lasPingDate, bool currentStatus)
    {
        var minuteAgo = TimeSpan.FromMinutes(1);
        var isLight = DateTime.UtcNow - lasPingDate <= minuteAgo;

        return isLight != currentStatus;
    }

    private async Task statusChanged(DynamoDbRepository repo, PingConfig config, PingInfo ping, Change? lastChange, Config? device)
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
        if (device != null)
        {
            var bot = new ChatBot(config.Token);
            var message = isLight ? MessageBuilder.GetLightOnMessage(timespan) : MessageBuilder.GetLightOffMessage(timespan);
            await bot.Post(message, device.ChatId);
        }
    }
}