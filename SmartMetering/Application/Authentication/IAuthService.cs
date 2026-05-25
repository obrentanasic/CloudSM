namespace SmartMetering.Application.Authentication;

public interface IAuthService
{
    Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task SetPasswordAsync(SetPasswordRequest request, CancellationToken ct = default);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
