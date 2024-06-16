using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;

namespace PingLight.Core.Config
{
    public class ConfigBuilder
    {
        private static string TOKEN_PARAM_NAME = "PingLight.TelegramBot.Token";

        public static async Task<PingConfig> Build(string stage, ILambdaLogger logger)
        {
            try
            {
                var config = new PingConfig();

                var client = new AmazonSimpleSystemsManagementClient();

                var request = new GetParameterRequest()
                {
                    Name = $"{TOKEN_PARAM_NAME}:{stage}"
                };
                var result = await client.GetParameterAsync(request);
                config.Token = result.Parameter.Value;

                return config;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());

                return new PingConfig();
            }
        }
    }
}
