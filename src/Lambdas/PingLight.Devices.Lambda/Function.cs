using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using PingLight.Core.DeviceConfig;
using Newtonsoft.Json;
using PingLight.Core.HttpResponses;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PingLight.Devices.Lambda;

public class Function
{
    private string Stage { get; init; }

    public Function()
    {
        Stage = Environment.GetEnvironmentVariable("STAGE") ?? throw new Exception("Environment variable STAGE is absent.");
    }

    public async Task<APIGatewayProxyResponse> GetDevicesAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var deviceRepo = new DeviceConfigRepository(Stage, context.Logger);

            var devices = await deviceRepo.GetConfigs();

            return new SuccessResponse(JsonConvert.SerializeObject(devices));
        }
        catch (Exception ex)
        {
            return new FailResponse(ex.Message);
        }
    }
}
