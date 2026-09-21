using Microsoft.AspNetCore.Authorization;
using PingLight.WebApi.Users;

namespace PingLight.WebApi.Authorization;

public sealed class SystemAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "SystemAdmin";

    public SystemAdminAttribute() => Policy = PolicyName;
}

public sealed class SystemAdminRequirement : IAuthorizationRequirement;

public sealed class SystemAdminAuthorizationHandler(IUserStore users) : AuthorizationHandler<SystemAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        SystemAdminRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return;

        var email = context.User.FindFirst("email")?.Value ?? context.User.FindFirst("username")?.Value ?? userId;
        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        var user = await users.GetOrCreateAsync(userId, email, cancellationToken);
        if (user.IsSystemAdmin) context.Succeed(requirement);
    }
}
