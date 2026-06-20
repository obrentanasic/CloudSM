using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Authentication;

namespace SmartMetering.WebApi.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Admin-only: creates a user and emails an activation link. No public registration.</summary>
    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var id = await _auth.CreateUserAsync(request, ct);
        return Ok(new { id });
    }

    /// <summary>Admin-only: lists all user accounts for the management panel.</summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken ct) =>
        Ok(await _auth.GetUsersAsync(ct));

    /// <summary>Admin-only: suspends a user account (blocks login).</summary>
    [HttpPost("users/{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        await _auth.SuspendUserAsync(CurrentUserId.Value, id, ct);
        return NoContent();
    }

    /// <summary>Admin-only: lifts a suspension.</summary>
    [HttpPost("users/{id:guid}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken ct)
    {
        await _auth.ReactivateUserAsync(id, ct);
        return NoContent();
    }

    /// <summary>Admin-only: permanently deletes a user account that has no linked data.</summary>
    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        await _auth.DeleteUserAsync(CurrentUserId.Value, id, ct);
        return NoContent();
    }

    [HttpPost("set-password")]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request, CancellationToken ct)
    {
        await _auth.SetPasswordAsync(request, ct);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _auth.LoginAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request, ct);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }
}
