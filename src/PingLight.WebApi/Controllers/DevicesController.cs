using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingLight.WebApi.Devices;

namespace PingLight.WebApi.Controllers;

[ApiController, Authorize, Route("devices")]
public sealed class DevicesController(IDeviceStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DevicePage>> List(CancellationToken cancellationToken)
    {
        var owner = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(owner)) return Unauthorized();
        return Ok(await store.ListAsync(owner, cancellationToken));
    }

    [HttpPut("{deviceId}/destinations/{chatId}/settings")]
    public async Task<IActionResult> Update(string deviceId, string chatId, DeviceSettings settings, CancellationToken cancellationToken)
    {
        var owner = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(owner)) return Unauthorized();
        if (deviceId.Length > 2048 || chatId.Length > 1024) return BadRequest();
        return await store.UpdateAsync(owner, deviceId, chatId, settings, cancellationToken) ? NoContent() : NotFound();
    }
}
