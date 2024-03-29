﻿using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;

namespace PingLight.Core.Config
{
    public class ConfigBuilder
    {
        private static string TOKEN_PARAM_NAME = "PingLight.TelegramBot.Token";
        private static string TEST_LABEL = ":test";
        private static string PROD_LABEL = ":prod";

        public static async Task<PingConfig> Build(bool isProd, ILambdaLogger logger)
        {
            try
            {
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
