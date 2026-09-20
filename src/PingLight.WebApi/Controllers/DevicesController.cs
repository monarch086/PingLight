using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingLight.WebApi.Devices;
using PingLight.WebApi.Users;

namespace PingLight.WebApi.Controllers;

[ApiController, Authorize, Route("devices")]
public sealed class DevicesController(IDeviceStore devices, IUserStore users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DevicePage>> List(CancellationToken cancellationToken)
    {
        var user = await CurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        return Ok(await devices.ListAsync(user.IsSystemAdmin ? null : user.GrantKeys, cancellationToken));
    }

    [HttpPut("{deviceId}/destinations/{chatId}/settings")]
    public async Task<IActionResult> Update(string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken)
    {
        var user = await CurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        if (deviceId.Length > 2048 || chatId.Length > 1024) return BadRequest();
        if (!user.IsSystemAdmin && !user.GrantKeys.Contains(UserGrantKey.Encode(deviceId, chatId))) return NotFound();
        return await devices.UpdateAsync(deviceId, chatId, settings, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPut("{deviceId}/destinations/{chatId}/notifications")]
    public async Task<IActionResult> SetNotifications(string deviceId, string chatId, NotificationState state,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        if (deviceId.Length > 2048 || chatId.Length > 1024) return BadRequest();
        if (!user.IsSystemAdmin && !user.GrantKeys.Contains(UserGrantKey.Encode(deviceId, chatId))) return NotFound();
        return await devices.SetActiveAsync(deviceId, chatId, state.IsActive, cancellationToken) ? NoContent() : NotFound();
    }

    private async Task<UserAccess?> CurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("username")?.Value ?? userId;
        return await users.GetOrCreateAsync(userId, email, cancellationToken);
    }
}
