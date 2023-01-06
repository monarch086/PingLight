using Telegram.Bot;
using Telegram.Bot.Types;

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
            var t = await client.SendTextMessageAsync(channelId, message, Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public async Task PostImage(string fileName, string text, string channelId)
        {
            Message message;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var finalPath = Path.Combine(basePath, fileName);

            using (Stream stream = System.IO.File.OpenRead(finalPath))
            {
                message = await postImage(stream, text, channelId);
            }
        }

        public async Task PostImageBytes(byte[] buffer, string text, string channelId)
        {
            Message message;

            using (Stream stream = new MemoryStream(buffer))
            {
                message = await postImage(stream, text, channelId);
            }
        }

        private async Task<Message> postImage(Stream stream, string text, string channelId)
        {
            return await client.SendPhotoAsync(
                chatId: channelId,
                photo: stream,
                caption: text
            );
        }
    }
}