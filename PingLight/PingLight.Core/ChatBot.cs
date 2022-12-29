using System.Text;
using Telegram.Bot;

namespace PingLight.Core
{
    public class ChatBot
    {
        private TelegramBotClient client;

        public ChatBot(string token)
        {
            client = new TelegramBotClient(token);
        }

        public async Task Post(string message, string channelId)
        {
            var t = await client.SendTextMessageAsync(channelId, message);
        }

        public string GetLightOnMessage(TimeSpan timeSpan)
        {
            return $"\U00002728 Є світло!\nСвітло було відсутнє протягом{getTimePart(timeSpan)}.";
        }

        public string GetLightOffMessage(TimeSpan timeSpan)
        {
            return $"\U0000203C Нема світла((\nСвітло було протягом{getTimePart(timeSpan)}.";
        }

        private string getTimePart(TimeSpan timeSpan)
        {
            var stringBuilder = new StringBuilder();

            if (timeSpan.Days > 0) stringBuilder.Append($" {timeSpan.Days} днів");

            if (timeSpan.Hours > 0) stringBuilder.Append($" {timeSpan.Hours} годин");

            if (timeSpan.Minutes > 0) stringBuilder.Append($" {timeSpan.Minutes} хвилин");

            return stringBuilder.ToString();
        }
    }
}