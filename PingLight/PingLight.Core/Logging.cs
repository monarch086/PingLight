using Telegram.Bot.Types;

namespace PingLight.Core
{
    public static class Logging
    {
        private static string fileName = "log.txt";
        private static string format = "dd MMM yyyy hh:mm tt";

        public static void LogUpdate(bool successPing)
        {
            var message = $"{DateTime.UtcNow.ToString(format)} -- light is {(successPing ? "present" : "absent")}";

            System.IO.File.AppendAllLines(fileName, new string[] { message });
        }

        public static void LogError(Exception ex)
        {
            var message = $"[ERROR] {DateTime.UtcNow.ToString(format)} -- {ex}.";

            System.IO.File.AppendAllLines(fileName, new string[] { message });
        }
    }
}
