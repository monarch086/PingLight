using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingLight.WebApi.Authorization;
using PingLight.WebApi.Devices;
using PingLight.WebApi.Users;

namespace PingLight.WebApi.Controllers;

[ApiController, Authorize, Route("users")]
public sealed class UsersController(IUserStore users, IUserDirectory directory, IDeviceStore devices) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserView>> Me(CancellationToken cancellationToken)
    {
        var user = await CurrentUser(cancellationToken);
        return user is null ? Unauthorized() : Ok(new CurrentUserView(user.UserId, user.Email, user.IsSystemAdmin));
    }

    [HttpGet, SystemAdmin]
    public async Task<ActionResult<UserPage>> List(CancellationToken cancellationToken)
    {
        var accounts = await directory.ListAsync(cancellationToken);
        var records = await Task.WhenAll(accounts.Select(account =>
            users.GetOrCreateAsync(account.UserId, account.Email, cancellationToken)));
        return Ok(new UserPage(records.Select(user =>
            new UserView(user.UserId, user.Email, user.IsSystemAdmin, user.Grants)).ToArray()));
    }

    [HttpPut("{userId}/devices/{deviceId}/destinations/{chatId}"), SystemAdmin]
    public async Task<IActionResult> Grant(string userId, string deviceId, string chatId, CancellationToken cancellationToken)
    {
        if (!Valid(userId, deviceId, chatId)) return BadRequest();
        if (!await devices.ExistsAsync(deviceId, chatId, cancellationToken)) return NotFound();
        var account = await directory.FindAsync(userId, cancellationToken);
        if (account is null) return NotFound();
        await users.GetOrCreateAsync(account.UserId, account.Email, cancellationToken);
        return await users.SetGrantAsync(userId, deviceId, chatId, true, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("{userId}/devices/{deviceId}/destinations/{chatId}"), SystemAdmin]
    public async Task<IActionResult> Revoke(string userId, string deviceId, string chatId, CancellationToken cancellationToken)
    {
        if (!Valid(userId, deviceId, chatId)) return BadRequest();
        if (await directory.FindAsync(userId, cancellationToken) is null) return NotFound();
        return await users.SetGrantAsync(userId, deviceId, chatId, false, cancellationToken) ? NoContent() : NotFound();
    }

    private async Task<UserAccess?> CurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("username")?.Value ?? userId;
        return await users.GetOrCreateAsync(userId, email, cancellationToken);
    }

    private static bool Valid(string userId, string deviceId, string chatId) =>
        userId.Length is > 0 and <= 128 && deviceId.Length is > 0 and <= 2048 && chatId.Length is > 0 and <= 1024;
}
