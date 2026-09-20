namespace PingLight.WebApi.Users;

public sealed record DeviceGrant(string DeviceId, string ChatId);

public sealed record UserAccess(
    string UserId,
    string Email,
    bool IsSystemAdmin,
    IReadOnlySet<string> GrantKeys)
{
    public IReadOnlyList<DeviceGrant> Grants => GrantKeys.Select(UserGrantKey.Decode).ToArray();
}

public sealed record CurrentUserView(string UserId, string Email, bool IsSystemAdmin);
public sealed record UserView(string UserId, string Email, bool IsSystemAdmin, IReadOnlyList<DeviceGrant> Grants);
public sealed record UserPage(IReadOnlyList<UserView> Items);

public interface IUserStore
{
    Task<UserAccess> GetOrCreateAsync(string userId, string email, CancellationToken cancellationToken);
    Task<bool> SetGrantAsync(string userId, string deviceId, string chatId, bool granted, CancellationToken cancellationToken);
}

public sealed record DirectoryUser(string UserId, string Email);

public interface IUserDirectory
{
    Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken);
    Task<DirectoryUser?> FindAsync(string userId, CancellationToken cancellationToken);
}

public static class UserGrantKey
{
    public static string Encode(string deviceId, string chatId) =>
        $"{Base64UrlEncode(deviceId)}.{Base64UrlEncode(chatId)}";

    public static DeviceGrant Decode(string key)
    {
        var separator = key.IndexOf('.');
        if (separator < 0) throw new FormatException("Invalid device grant key.");
        return new(Base64UrlDecode(key[..separator]), Base64UrlDecode(key[(separator + 1)..]));
    }

    private static string Base64UrlEncode(string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '=');
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
