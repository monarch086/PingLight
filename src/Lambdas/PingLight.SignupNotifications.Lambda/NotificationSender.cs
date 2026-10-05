using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Net;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace PingLight.SignupNotifications.Lambda;

public sealed record NotificationSettings(string Stage, string ProjectName, string EmailFrom,
    string EmailTo, string TelegramChatId, string TelegramTokenParameter, string DevicesUrl);

public sealed class NotificationSender(IAmazonSimpleEmailService ses, IAmazonSimpleSystemsManagement ssm,
    HttpClient http, NotificationSettings settings) : INotificationSender
{
    private string? telegramToken;

    public static string MessageFor(SignupNotification notification, NotificationSettings settings) =>
        $"New user signed up\nEnvironment: {settings.Stage}\nProject: {settings.ProjectName}\nUser email: {notification.Email}";

    public static string EmailMessageFor(SignupNotification notification, NotificationSettings settings)
    {
        if (notification.Kind == "signup")
            return $"Зареєструвався новий користувач\nСередовище: {settings.Stage}\nПроєкт: {settings.ProjectName}\nЕлектронна пошта користувача: {notification.Email}";
        return AccessChangeMessage(notification) + $"\n\nПерейти до моїх пристроїв: {DevicesUrl(settings)}";
    }

    private static string AccessChangeMessage(SignupNotification notification)
    {
        if (notification.Kind != "access-change" || notification.AccessChanges is not { Count: > 0 })
            throw new InvalidOperationException("Unsupported email notification.");

        var message = new StringBuilder("Вітаємо!\n\nВаш доступ до пристроїв змінено.\n");
        foreach (var granted in new[] { true, false })
        {
            var changes = notification.AccessChanges.Where(change => change.Granted == granted).ToArray();
            if (changes.Length == 0) continue;
            message.AppendLine().AppendLine(granted ? "Вам надано доступ до таких пристроїв:" : "Ваш доступ до таких пристроїв скасовано:");
            foreach (var deviceId in changes.Select(change => change.DeviceId).Distinct(StringComparer.Ordinal))
                message.AppendLine($"• Пристрій: {deviceId}");
        }
        return message.ToString();
    }

    public static string AccessEmailHtmlFor(SignupNotification notification, NotificationSettings settings)
    {
        var details = WebUtility.HtmlEncode(AccessChangeMessage(notification)).Replace("\r\n", "\n").Replace("\n", "<br>");
        var url = WebUtility.HtmlEncode(DevicesUrl(settings));
        return $"""
            <!doctype html>
            <html lang="uk"><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#222;line-height:1.6">
            <p>{details}</p>
            <p><a href="{url}" style="display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:12px 20px;border-radius:6px">Перейти до моїх пристроїв</a></p>
            </body></html>
            """;
    }

    private static string DevicesUrl(NotificationSettings settings)
    {
        if (!Uri.TryCreate(settings.DevicesUrl, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Configure an HTTPS devices URL.");
        return url.AbsoluteUri;
    }

    public async Task SendAsync(SignupNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Channel == "email")
        {
            await ses.SendEmailAsync(new()
            {
                Source = settings.EmailFrom,
                Destination = new() { ToAddresses = [notification.Kind == "access-change" ? notification.Email : settings.EmailTo] },
                Message = new()
                {
                    Subject = new() { Data = notification.Kind == "access-change" ? "Зміна доступу до пристроїв" :
                        $"[{settings.Stage}] {settings.ProjectName}: новий користувач", Charset = "UTF-8" },
                    Body = new()
                    {
                        Text = new() { Data = EmailMessageFor(notification, settings), Charset = "UTF-8" },
                        Html = notification.Kind == "access-change" ?
                            new() { Data = AccessEmailHtmlFor(notification, settings), Charset = "UTF-8" } : null
                    }
                }
            }, cancellationToken);
        }
        else if (notification.Channel == "telegram")
        {
            if (notification.Kind != "signup") throw new InvalidOperationException("Unsupported Telegram notification.");
            telegramToken ??= (await ssm.GetParameterAsync(new GetParameterRequest
            {
                Name = settings.TelegramTokenParameter, WithDecryption = true
            }, cancellationToken)).Parameter.Value;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{telegramToken}/sendMessage",
                new { chat_id = settings.TelegramChatId, text = MessageFor(notification, settings) }, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Telegram delivery failed.");
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token),
                cancellationToken: timeout.Token);
            if (!body.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                throw new InvalidOperationException("Telegram delivery failed.");
        }
        else throw new InvalidOperationException("Unsupported notification channel.");
    }
}
