using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Text.Json.Nodes;
using PingLight.Core.Persistence;
using PingLight.Core.HttpResponses;
using PingLight.Core.DeviceConfig;
using PingLight.Core.Model;
using PingLight.Core;
using PingLight.Core.SsmConfig;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.GatherApi.Lambda;

public class Function
{
    public async Task<APIGatewayProxyResponse> AddPingAsync(JsonObject input, ILambdaContext context)
    {
        try
        {
            var inputData = input["queryStringParameters"].Deserialize<PingInputModel>();
            if (inputData == null) { return new BadRequestResponse("Failed to deserialize input model."); }

            var stage = Environment.GetEnvironmentVariable("STAGE");
            var pingsRepository = new PingsRepository(stage, context.Logger);

            await pingsRepository.AddPing(inputData.Id);

            return new SuccessResponse("Ping was saved successfully.");
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.ToString());
        }
    }

    public async Task<APIGatewayProxyResponse> AddChangeAsync(JsonObject input, ILambdaContext context)
    {
        try
        {
            var inputData = input["queryStringParameters"].Deserialize<ChangeInputModel>();
            if (inputData == null) { return new BadRequestResponse("Failed to deserialize input model."); }

            var stage = Environment.GetEnvironmentVariable("STAGE");
            var changesRepo = new ChangesRepository(stage, context.Logger);
            var pingsRepository = new PingsRepository(stage, context.Logger);

            var deviceRepo = new DeviceConfigRepository(stage, context.Logger);
            var devices = await deviceRepo.GetConfigs();
            var device = devices.FirstOrDefault(d => d.DeviceId == inputData.Id);

            if (device == null)
            {
                return new BadRequestResponse($"Device with id = {inputData.Id} not found.");
            }

            var ping = await pingsRepository.GetPing(inputData.Id);
            if (ping != null)
            {
                return new BadRequestResponse($"For device with id = {inputData.Id} ping mode is enabled.");
            }

            var lastChange = await changesRepo.GetLatestChange(inputData.Id);
            if (lastChange != null && lastChange.IsLight == inputData.IsLight)
            {
                return new BadRequestResponse($"For device with id = {inputData.Id} last change has the same state.");
            }

            await changesRepo.AddChange(new Change
            {
                DeviceId = inputData.Id,
                ChangeDate = DateTime.UtcNow,
                IsLight = inputData.IsLight
            });

            if (device.IsActive)
            {
                var timespan = lastChange != null ? DateTime.UtcNow - lastChange.ChangeDate : TimeSpan.Zero;

                var config = await SsmConfigBuilder.Build(stage, context.Logger);
                var bot = new ChatBot(config.Token);
                var message = inputData.IsLight ? MessageBuilder.GetLightOnMessage(timespan) : MessageBuilder.GetLightOffMessage(timespan);
                var success = await bot.Post(message, device.ChatId);
                context.Logger.LogInformation($"Posting success: {success}");
            }

            return new SuccessResponse("Change was saved successfully.");
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.ToString());
        }
    }
}
