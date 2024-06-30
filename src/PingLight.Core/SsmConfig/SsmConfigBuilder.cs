using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;
using PingLight.Core.Config;

namespace PingLight.Core.SsmConfig
{
    public class SsmConfigBuilder
    {
        private static string APP_NAME = "PingLight";
        private static string TOKEN_PARAM_NAME = "TelegramBot.Token";

        public static async Task<PingConfig> Build(string stage, ILambdaLogger logger)
        {
            try
            {
                var config = new PingConfig();

                var client = new AmazonSimpleSystemsManagementClient();

                var request = new GetParameterRequest()
                {
                    Name = $"/{APP_NAME}/{stage}/{TOKEN_PARAM_NAME}"
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
