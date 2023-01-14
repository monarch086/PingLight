using Microsoft.Extensions.Configuration;
using PingLight.Core;

namespace PingLight.App.Config
{
    internal static class ConfigReader
    {
        private static string PROD_SUFIX = ".prod";

        public static Config Read(bool isProd)
        {
            var fileName = $"appsettings{(isProd ? PROD_SUFIX : "")}.json";

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(fileName, optional: false);

                IConfiguration settings = builder.Build();
                var config = new Config();

                var pingSection = settings.GetSection("PingSettings");
                if (pingSection != null)
                {
                    config.Host = pingSection["Host"];
                    config.DelayMillis = int.Parse(pingSection["DelayMillis"]);
                }

                var telegramSection = settings.GetSection("TelegramSettings");
                if (telegramSection != null)
                {
                    config.Token = telegramSection["Token"];
                    config.ChannelId = telegramSection["ChannelId"];
                }

                return config;
            }
            catch (Exception ex)
            {
                Logging.LogError(ex);

                return new Config();
            }
        }
    }
}
