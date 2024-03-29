﻿using Amazon.Lambda.Core;
using Ical.Net;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.TurnOffNotifications.Lambda;

public class Function
{
    /// <summary>
    /// A function that sends notifications about electricity turning off
    /// </summary>
    private const int MINUTES_TO_EVENT_TO_INCLUDE = 185;
    private const int MINUTES_TO_EVENT_TO_SKIP = 150;
    private ScheduleLoader scheduleLoader = new ScheduleLoader();

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var isProd = input.IsProduction();
        var config = await ConfigBuilder.Build(isProd, context.Logger);

        var devicesRepo = new DeviceConfigRepository(isProd, context.Logger);
        var devices = (await devicesRepo.GetConfigs()).Where(d => d.TurnOffGroup.HasValue);

        foreach (var device in devices)
        {
            await processDeviceAsync(device, config, context.Logger);
        }
    }

    private async Task processDeviceAsync(Config device, PingConfig config, ILambdaLogger logger)
    {
        var groupNumber = device.TurnOffGroup.Value;
        var icsSchedule = await scheduleLoader.GetOrLoadScheduleAsync(groupNumber, logger);

        var calendar = Calendar.Load(icsSchedule);

        var nextEvent = calendar.Events.FirstOrDefault(e => e.DtStart.AsUtc > DateTime.UtcNow
                                                        && (e.DtStart.AsUtc - DateTime.UtcNow).TotalMinutes < MINUTES_TO_EVENT_TO_INCLUDE
                                                        && (e.DtStart.AsUtc - DateTime.UtcNow).TotalMinutes > MINUTES_TO_EVENT_TO_SKIP);

        if (nextEvent != null)
        {
            var startTime = nextEvent.DtStart.AsUtc.ToKyivTime();
            var endTime = nextEvent.DtEnd.AsUtc.ToKyivTime();
            var message = MessageBuilder.GetTurnOffNotificationMessage(startTime, endTime, groupNumber);

            logger.LogInformation(message);

            var bot = new ChatBot(config.Token);
            await bot.Post(message, device.ChatId);
        }
    }
}
