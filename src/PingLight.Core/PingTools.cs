using System.Net.NetworkInformation;
using System.Text;

namespace PingLight.Core
{
    public class PingTools
    {
        private static string Data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; // 32 bytes
        private static int PingTimeout = 120;
        private static int RetryAttempts = 3;
        private static int RetryDelay = 10000; // 10 sec

        public static bool Ping(string host)
        {
            var pingSender = new Ping();
            var options = new PingOptions();

            options.DontFragment = true;

            var buffer = Encoding.ASCII.GetBytes(Data);

            for (int i = 0; i < RetryAttempts; i++)
            {
                var reply = pingSender.Send(host, PingTimeout, buffer, options);
                var response = reply.Status == IPStatus.Success;

                if (response) return response;

                Task.Delay(RetryDelay);
            }

            return false;
        }
    }
}
