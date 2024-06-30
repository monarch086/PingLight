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
    private readonly string stage;
    private readonly IConfiguration configuration;

    public Function()
    {
        stage = Environment.GetEnvironmentVariable("STAGE");

        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        configuration = builder.Build();
    }

    public async Task FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var configsRepository = new ConfigsRepository(stage, context.Logger);
        var groupResolver = new FlyDevGroupResolver(configuration);
        var scheduleLoader = new ScheduleLoader(groupResolver, context.Logger);
        var customScheduleLoader = new CustomScheduleLoader(configsRepository, context.Logger);

        var config = await SsmConfigBuilder.Build(stage, context.Logger);

        var devicesRepo = new DeviceConfigRepository(stage, context.Logger);
        var devices = (await devicesRepo.GetConfigs()).Where(d => !string.IsNullOrEmpty(d.TurnOffGroup));

        foreach (var device in devices)
        {
            if (!device.IsActive || string.IsNullOrEmpty(device.TurnOffGroup)) continue;

            await processDeviceAsync(device, config, scheduleLoader, customScheduleLoader, context.Logger);
        }
    }

    private async Task processDeviceAsync(Config device, PingConfig config, ScheduleLoader scheduleLoader, CustomScheduleLoader customScheduleLoader, ILambdaLogger logger)
    {
        var groupNumber = device.TurnOffGroup;

        var icsSchedule = device.UseCustomCalendar
            ? await customScheduleLoader.GetOrLoadScheduleAsync(groupNumber)
            : await scheduleLoader.GetOrLoadScheduleAsync(groupNumber);

        var calendar = Calendar.Load(icsSchedule);

        var searchStart = DateTime.UtcNow.AddMinutes(device.TurnOffPeriodMinutes - 5);
        var searchEnd = DateTime.UtcNow.AddMinutes(device.TurnOffPeriodMinutes + 5);

        var occurrences = calendar.GetOccurrences(searchStart, searchEnd);

        var nextEvent = occurrences.FirstOrDefault(o => (o.Period.StartTime.AsUtc - DateTime.UtcNow).TotalMinutes < (device.TurnOffPeriodMinutes + 5) &&
                                                        (o.Period.StartTime.AsUtc - DateTime.UtcNow).TotalMinutes > (device.TurnOffPeriodMinutes - 5));

        if (nextEvent != null)
        {
            var startTime = nextEvent.Period.StartTime.AsUtc.ToKyivTime();
            var endTime = nextEvent.Period.EndTime.AsUtc.ToKyivTime();
            var message = MessageBuilder.GetTurnOffNotificationMessage(startTime, endTime);

            logger.LogInformation(message);

            var bot = new ChatBot(config.Token);
            await bot.Post(message, device.ChatId);
        }
    }
}
