using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        //public static async Task<bool> PingHttp(string host)
        //{
        //    var client = new HttpClient();
        //    client.Timeout = TimeSpan.FromSeconds(5);
        //    client.BaseAddress = new Uri(host);
        //    var request = new HttpRequestMessage(HttpMethod.Get, "");

        //    try
        //    {
        //        var response = await client.SendAsync(request);
        //        return response.IsSuccessStatusCode;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        //public static bool PingTcp(string host)
        //{
        //    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    bool ConState = true;
        //    sock.LingerState = new LingerOption(true, 2);
        //    sock.NoDelay = true;
        //    char[] delimiterChars = { ':' };
        //    string[] srv = host.Split(delimiterChars);
        //    try
        //    { sock.Connect(srv[0], int.Parse(srv[1])); }
        //    catch (SocketException ex)
        //    { ConState = false; }
        //    catch (Exception ex)
        //    { ConState = false; }
        //    finally
        //    {
        //        if (sock.Connected)
        //        {
        //            sock.Close();
        //            ConState = true;
        //        }
        //        else
        //        { ConState = false; }
        //    }
        //    if (ConState == true)
        //    {
        //        Thread.Sleep(250);
        //        return true;
        //    }
        //    else { return false; }
        //}
    }
}
