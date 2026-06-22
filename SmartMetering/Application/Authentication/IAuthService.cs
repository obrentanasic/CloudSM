namespace SmartMetering.Application.Authentication;

public interface IAuthService
{
    Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task SetPasswordAsync(SetPasswordRequest request, CancellationToken ct = default);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    // ── Admin user management ────────────────────────────────────────────────
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);

    Task ResendActivationAsync(Guid userId, CancellationToken ct = default);

    Task SuspendUserAsync(Guid actingUserId, Guid userId, CancellationToken ct = default);

    Task ReactivateUserAsync(Guid userId, CancellationToken ct = default);

    Task DeleteUserAsync(Guid actingUserId, Guid userId, CancellationToken ct = default);
}
