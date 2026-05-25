using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Authentication;

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    UserRole Role);

public sealed record SetPasswordRequest(string Token, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, string Email, string FullName, string Role);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>Frontend base URL used to build activation / reset links in emails.</summary>
public sealed class AuthLinkOptions
{
    public string ClientBaseUrl { get; set; } = "http://localhost:5173";
}
