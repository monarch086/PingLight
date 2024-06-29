using Amazon.Lambda.Core;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Model;
using PingLight.Core.Persistence;
using PingLight.Core.SsmConfig;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.AggregateChanges.Lambda;

public class Function
{
    /// <summary>
    /// A function that finds out if there was a change in status and posts updates to TG
    /// </summary>
    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");
        var config = await SsmConfigBuilder.Build(stage, context.Logger);
        var pingsRepo = new PingsRepository(stage, context.Logger);
        var changesRepo = new ChangesRepository(stage, context.Logger);
        var deviceRepo = new DeviceConfigRepository(stage, context.Logger);

        var devices = await deviceRepo.GetConfigs();
        var pings = await pingsRepo.GetPings();

        foreach (var device in devices)
        {
            var change = await changesRepo.GetLatestChange(device.DeviceId);
            var ping = pings.FirstOrDefault(p => device.DeviceId == p.Id);
            if (ping == null)
            {
                context.Logger.LogInformation($"No ping found for device {device.DeviceId}.");
                continue;
            }

            var changed = change == null || isChanged(ping.LastPingDate, change.IsLight, device.NotificationDelaySec);
            if (changed) await statusChanged(context.Logger, changesRepo, config, ping, change, device);
        }

        context.Logger.LogInformation("Pings processing complete.");
    }

    private bool isChanged(DateTime lasPingDate, bool currentStatus, int delaySec)
    {
        var delay = TimeSpan.FromSeconds(delaySec);
        var isLight = DateTime.UtcNow - lasPingDate <= delay;

        return isLight != currentStatus;
    }

    private async Task statusChanged(ILambdaLogger logger, ChangesRepository changesRepo, PingConfig config, PingInfo ping, Change? lastChange, Config? device)
    {
        var isLight = lastChange != null ? !lastChange.IsLight : true;
        var timespan = lastChange != null ? DateTime.UtcNow - lastChange.ChangeDate : TimeSpan.Zero;

        // Add change to DB
        await changesRepo.AddChange(new Change 
        {
            DeviceId = ping.Id,
            ChangeDate = DateTime.UtcNow,
            IsLight = isLight
        });

        // Post to TG
        if (device != null && device.IsActive)
        {
            var bot = new ChatBot(config.Token);
            var message = isLight ? MessageBuilder.GetLightOnMessage(timespan) : MessageBuilder.GetLightOffMessage(timespan);
            var success = await bot.Post(message, device.ChatId);
            logger.LogInformation($"Posting success: {success}");
        }
    }
}