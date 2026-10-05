using System.Net;
using System.Text.Json.Nodes;

namespace PingLight.SignupNotifications.Lambda;

// Cognito substitutes codeParameter and usernameParameter after this hook returns.
public sealed class CognitoMessages
{
    public JsonObject Customize(JsonObject message)
    {
        var source = message["triggerSource"]?.GetValue<string>();
        if (source?.StartsWith("CustomMessage_", StringComparison.Ordinal) != true) return message;
        var code = message["request"]?["codeParameter"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Missing Cognito code placeholder.");
        var (subject, text) = source switch
        {
            "CustomMessage_ForgotPassword" => ("Відновлення пароля", $"Ваш код для відновлення пароля: {code}"),
            "CustomMessage_AdminCreateUser" => ("Запрошення до сервісу",
                $"Вам створено обліковий запис. Ім’я користувача: {message["request"]?["usernameParameter"]?.GetValue<string>()}. Тимчасовий пароль: {code}"),
            "CustomMessage_Authentication" => ("Підтвердження входу", $"Ваш код для входу: {code}"),
            "CustomMessage_UpdateUserAttribute" or "CustomMessage_VerifyUserAttribute" =>
                ("Підтвердження електронної пошти", $"Ваш код для підтвердження електронної пошти: {code}"),
            _ => ("Підтвердження реєстрації", $"Ваш код для підтвердження реєстрації: {code}")
        };
        var response = message["response"]?.AsObject();
        if (response is null) message["response"] = response = new JsonObject();
        response["emailSubject"] = subject;
        response["emailMessage"] = $"<p>Вітаємо!</p><p>{WebUtility.HtmlEncode(text)}</p><p>Якщо ви не надсилали цей запит, проігноруйте цей лист.</p>";
        return message;
    }
}
