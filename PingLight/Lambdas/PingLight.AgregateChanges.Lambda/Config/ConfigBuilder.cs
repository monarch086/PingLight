using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.Configuration;

namespace PingLight.AggregateChanges.Lambda.Config
{
    internal class ConfigBuilder
    {
        private static string PROD_SUFIX = ".prod";
        private static string TOKEN_PARAM_NAME = "PingLight.TelegramBot.Token";
        private static string TEST_LABEL = ":test";
        private static string PROD_LABEL = ":prod";

        public static async Task<PingConfig> Build(bool isProd, ILambdaLogger logger)
        {
            var fileName = $"appsettings{(isProd ? PROD_SUFIX : "")}.json";

            try
            {
                //var builder = new ConfigurationBuilder()
                    //.SetBasePath(Directory.GetCurrentDirectory())
                    //.AddJsonFile(fileName, optional: false)
                    //.AddSystemsManager(path: "/", reloadAfter: TimeSpan.FromMinutes(10));

                //IConfiguration settings = builder.Build();

                var config = new PingConfig();

                var client = new AmazonSimpleSystemsManagementClient();

                var request = new GetParameterRequest()
                {
                    Name = $"{TOKEN_PARAM_NAME}{(isProd ? PROD_LABEL : TEST_LABEL)}"
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
