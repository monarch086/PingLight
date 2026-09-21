using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingLight.WebApi.Authorization;
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

    [HttpPut("{deviceId}/destinations/{chatId}/settings"), DeviceAccess]
    public async Task<IActionResult> Update(string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken)
    {
        return await devices.UpdateAsync(deviceId, chatId, settings, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPut("{deviceId}/destinations/{chatId}/notifications"), DeviceAccess]
    public async Task<IActionResult> SetNotifications(string deviceId, string chatId, NotificationState state,
        CancellationToken cancellationToken)
    {
        return await devices.SetActiveAsync(deviceId, chatId, state.IsActive, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("{deviceId}/destinations/{chatId}/turn-offs"), DeviceAccess]
    public async Task<ActionResult<TurnOffPage>> ListTurnOffs(string deviceId, string chatId, [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 1000) return BadRequest();
        if (!await devices.ExistsAsync(deviceId, chatId, cancellationToken)) return NotFound();
        return Ok(await devices.ListTurnOffsAsync(deviceId, page, cancellationToken));
    }

    private async Task<UserAccess?> CurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("username")?.Value ?? userId;
        return await users.GetOrCreateAsync(userId, email, cancellationToken);
    }
}
