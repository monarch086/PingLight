using PingLight.App.Config;
using PingLight.Core;
using System.Text;

namespace PingLight.App
{
    internal class Program
    {
        private static ChatBot? bot;

        private static State currentState;

        static Program()
        {
            currentState = StateManager.LoadOrCreateState();
        }

        static async Task Main(string[] args)
        {
            var isProd = IsProd(args);
            Console.WriteLine($"PingLight is running in {(isProd ? "PROD" : "NON-PROD")} mode.");

            var config = ConfigReader.Read(isProd);
            bot = new ChatBot(config.Token);

            while (true)
            {
                try
                {
                    var successPing = PingTools.Ping(config.Host);
                    var stateChanged = currentState.IsLight != successPing;
                    
                    if (stateChanged) { 
                        await HandleStateUpdate(successPing, config.ChannelId);
                    }

                    await Task.Delay(config.DelayMillis);
                }
                catch(Exception ex)
                {
                    Logging.LogError(ex);
                }
            }
        }

        private static bool IsProd(string[] args)
        {
            return args.Length > 0 && args.Contains("prod");
        }

        private static async Task HandleStateUpdate(bool successPing, string channelId)
        {
            var timeSpan = DateTime.UtcNow - currentState.LastChangeTime;

            currentState.LastChangeTime = DateTime.UtcNow;
            currentState.IsLight = successPing;
            StateManager.SaveState(currentState);

            var message = successPing ? bot.GetLightOnMessage(timeSpan) : bot.GetLightOffMessage(timeSpan);

            await bot.Post(message, channelId);

            Logging.LogUpdate(successPing);
        }
    }
}