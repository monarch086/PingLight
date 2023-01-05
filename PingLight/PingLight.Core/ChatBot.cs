using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PingLight.Core
{
    public class ChatBot
    {
        private const string LIGHT_ICON = "✨";
        private const string EXCLAMATION_ICON = "‼";
        private const string STATS_ICON = "📊";

        private TelegramBotClient client;

        public ChatBot(string token)
        {
            client = new TelegramBotClient(token);
        }

        public async Task Post(string message, string channelId)
        {
            var t = await client.SendTextMessageAsync(channelId, message);
        }

        public async Task PostImage(string fileName, string text, string channelId)
        {
            Message message;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var finalPath = Path.Combine(basePath, fileName);

            using (Stream stream = System.IO.File.OpenRead(finalPath))
            {
                message = await client.SendPhotoAsync(
                    chatId: channelId,
                    photo: stream,
                    caption: text
                );
            }
        }

        public string GetLightOnMessage(TimeSpan timeSpan)
        {
            return $"{LIGHT_ICON} Є світло!\nСвітло було відсутнє протягом{getTimePart(timeSpan)}.";
        }

        public string GetLightOffMessage(TimeSpan timeSpan)
        {
            return $"{EXCLAMATION_ICON} Нема світла((\nСвітло було протягом{getTimePart(timeSpan)}.";
        }

        public string GetDailyStatsMessage(List<TimeSpan> blackouts)
        {
            var sb = new StringBuilder($"{STATS_ICON} Статистика:\n");

            if (blackouts.Count == 0)
            {
                sb.Append("За минулу добу не зафіксовано відключень світла.");
                return sb.ToString();
            }

            var total = blackouts.Aggregate((a, b) => a.Add(b));

            sb.Append($"За минулу добу світло було відключене {blackouts.Count} разів ");
            sb.Append($"на {total.Hours} годин {total.Minutes} хвилин.");

            return sb.ToString();
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