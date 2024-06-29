using Amazon.Lambda.Core;
using Ical.Net;
using Microsoft.Extensions.Configuration;
using PingLight.Core;
using PingLight.Core.Config;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Persistence;
using PingLight.Core.Schedules;
using PingLight.Core.SsmConfig;
using System.Text.Json.Nodes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.TurnOffNotifications.Lambda;

public class Function
{
    private ScheduleLoader scheduleLoader;
    private CustomScheduleLoader customScheduleLoader;

    private readonly FlyDevGroupResolver groupResolver;

    public Function()
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");

        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        groupResolver = new FlyDevGroupResolver(configuration);
    }

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var stage = Environment.GetEnvironmentVariable("STAGE");
        var config = await SsmConfigBuilder.Build(stage, context.Logger);
        
        var configsRepository = new ConfigsRepository(stage, context.Logger);
        scheduleLoader = new ScheduleLoader(groupResolver, context.Logger);
        customScheduleLoader = new CustomScheduleLoader(configsRepository, context.Logger);

        var devicesRepo = new DeviceConfigRepository(stage, context.Logger);
        var devices = (await devicesRepo.GetConfigs()).Where(d => !string.IsNullOrEmpty(d.TurnOffGroup));

        foreach (var device in devices)
        {
            if (!device.IsActive) continue;

            await processDeviceAsync(device, config, context.Logger);
        }
    }

    private async Task processDeviceAsync(Config device, PingConfig config, ILambdaLogger logger)
    {
        var groupNumber = device.TurnOffGroup;
        var icsSchedule = device.UseCustomCalendar
            ? await customScheduleLoader.GetOrLoadScheduleAsync(groupNumber)
            : await scheduleLoader.GetOrLoadScheduleAsync(groupNumber);

        var calendar = Calendar.Load(icsSchedule);

        var nextEvent = calendar.Events.FirstOrDefault(e => e.DtStart.AsUtc > DateTime.UtcNow
                                                        && (e.DtStart.AsUtc - DateTime.UtcNow).TotalMinutes < (device.TurnOffPeriodMinutes + 10)
                                                        && (e.DtStart.AsUtc - DateTime.UtcNow).TotalMinutes > (device.TurnOffPeriodMinutes - 10));

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
