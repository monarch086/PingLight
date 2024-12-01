using Amazon.Lambda.Core;
using System.Text;
using System.Text.Json;

namespace PingLight.Core
{
    public sealed class WhatsAppClient
    {
        private readonly string apiUrl = "https://gate.whapi.cloud/";
        private readonly HttpClient client;
        private readonly ILambdaLogger logger;

        public WhatsAppClient(string token, ILambdaLogger logger)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            this.logger = logger;
        }

        public async Task<bool> PostAsync(string message, string chatId)
        {
            var textMessageUrl = "messages/text";
            var payload = new
            {
                body = message,
                to = chatId,
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(textMessageUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation($"Sending message to {textMessageUrl}: {response.StatusCode}.");
            }

            return response.IsSuccessStatusCode;
        }
    }
}
