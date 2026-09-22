using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace PingLight.WebApi.Users;

public sealed class CognitoUserDirectory(IAmazonCognitoIdentityProvider cognito, IConfiguration configuration) : IUserDirectory
{
    private readonly string userPoolId = configuration["Cognito:UserPoolId"]
        ?? throw new InvalidOperationException("Configure Cognito:UserPoolId.");

    public async Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken)
    {
        string? token = null;
        var users = new List<DirectoryUser>();
        do
        {
            var response = await cognito.ListUsersAsync(new ListUsersRequest
            {
                UserPoolId = userPoolId,
                PaginationToken = token,
                Limit = 60
            }, cancellationToken);
            users.AddRange(response.Users.Select(Map));
            token = response.PaginationToken;
        } while (!string.IsNullOrEmpty(token));
        return users.OrderBy(user => user.Email, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<DirectoryUser?> FindAsync(string userId, CancellationToken cancellationToken)
        => (await ListAsync(cancellationToken)).SingleOrDefault(user => user.UserId == userId);

    private static DirectoryUser Map(UserType user) => Map(user.Username, user.Attributes);

    private static DirectoryUser Map(string username, IEnumerable<AttributeType> attributes)
    {
        var values = attributes.ToDictionary(attribute => attribute.Name, attribute => attribute.Value);
        var userId = values.GetValueOrDefault("sub") ?? username;
        return new(userId, values.GetValueOrDefault("email") ?? username);
    }
}
