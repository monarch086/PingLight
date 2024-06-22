using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PingLight.Core
{
    public class ChatBot
    {
        private TelegramBotClient client;

        public ChatBot(string token)
        {
            client = new TelegramBotClient(token);
        }

        public async Task<bool> Post(string message, string chatId)
        {
            var result = await client.SendTextMessageAsync(chatId, message, messageThreadId: null, ParseMode.Html);

            return result != null;
        }

        public async Task PostImage(string fileName, string text, string chatId)
        {
            Message message;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var finalPath = Path.Combine(basePath, fileName);

            using var stream = System.IO.File.OpenRead(finalPath);

            message = await postImage(stream, text, chatId);
        }

        public async Task PostImageBytes(byte[] buffer, string text, string chatId)
        {
            Message message;

            using var stream = new MemoryStream(buffer);

            message = await postImage(stream, text, chatId);
        }

        private async Task<Message> postImage(Stream stream, string text, string chatId)
        {
            return await client.SendPhotoAsync(
                chatId: chatId,
                photo: InputFile.FromStream(stream),
                caption: text,
                parseMode: ParseMode.Html
            );
        }
    }
}