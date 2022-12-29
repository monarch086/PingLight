using System.Text;
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

            Console.OutputEncoding = Encoding.UTF8;
            var color = Console.ForegroundColor;
            Console.ForegroundColor = successPing ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = color;

            System.IO.File.AppendAllLines(fileName, new string[] { message });
        }

        public static void LogError(Exception ex)
        {
            var message = $"[ERROR] {DateTime.UtcNow.ToString(format)} -- {ex}.";

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = color;

            System.IO.File.AppendAllLines(fileName, new string[] { message });
        }
    }
}
