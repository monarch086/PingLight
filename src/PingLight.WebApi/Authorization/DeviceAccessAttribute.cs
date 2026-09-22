using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PingLight.WebApi.Users;

namespace PingLight.WebApi.Authorization;

public sealed class DeviceAccessAttribute : TypeFilterAttribute
{
    public DeviceAccessAttribute() : base(typeof(DeviceAccessFilter)) { }
}

public sealed class DeviceAccessFilter(IUserStore users) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var deviceId = context.RouteData.Values["deviceId"]?.ToString() ?? "";
        var chatId = context.RouteData.Values["chatId"]?.ToString() ?? "";
        if (deviceId.Length > 2048 || chatId.Length > 1024)
        {
            context.Result = new BadRequestResult();
            return;
        }

        var email = context.HttpContext.User.FindFirst("email")?.Value
            ?? context.HttpContext.User.FindFirst("username")?.Value
            ?? userId;
        var user = await users.GetOrCreateAsync(userId, email, context.HttpContext.RequestAborted);
        if (!user.IsSystemAdmin && !user.GrantKeys.Contains(UserGrantKey.Encode(deviceId, chatId)))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
