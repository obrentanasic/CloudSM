using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IEmailService _email;
    private readonly AuthLinkOptions _links;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwt,
        IEmailService email,
        AuthLinkOptions links)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _email = email;
        _links = links;
    }

    public async Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, ct))
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = User.Create(request.FirstName, request.LastName, email, request.PhoneNumber, request.Role);
        var token = user.IssueSecurityToken(ActivationTokenLifetime);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        var link = $"{_links.ClientBaseUrl}/set-password?token={token}";
        await _email.SendAsync(
            user.Email,
            "Активирајте свој Smart Metering налог",
            $"<p>Поштовани {user.FullName},</p>" +
            $"<p>Креиран вам је налог. Кликните на линк да поставите лозинку (важи 24 сата):</p>" +
            $"<p><a href=\"{link}\">{link}</a></p>",
            ct);

        return user.Id.Value;
    }

    public async Task SetPasswordAsync(SetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetBySecurityTokenAsync(request.Token, ct)
            ?? throw new UnauthorizedAppException("Невалидан или истекао линк.");

        if (!user.IsSecurityTokenValid(request.Token))
        {
            throw new UnauthorizedAppException("Невалидан или истекао линк.");
        }

        user.SetPassword(_passwordHasher.Hash(request.Password));
        await _users.SaveChangesAsync(ct);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.CanLogin || !_passwordHasher.Verify(request.Password, user.PasswordHash!))
        {
            throw new UnauthorizedAppException("Погрешан имејл или лозинка.");
        }

        var token = _jwt.GenerateToken(user);
        return new LoginResponse(token, user.Email, user.FullName, user.Role.ToString());
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        // Silently succeed if the user doesn't exist, to avoid leaking which emails are registered.
        if (user is null || user.Status == UserStatus.Suspended)
        {
            return;
        }

        var token = user.IssueSecurityToken(ResetTokenLifetime);
        await _users.SaveChangesAsync(ct);

        var link = $"{_links.ClientBaseUrl}/reset-password?token={token}";
        await _email.SendAsync(
            user.Email,
            "Ресетовање лозинке - Smart Metering",
            $"<p>Поштовани {user.FullName},</p>" +
            $"<p>Кликните на линк да ресетујете лозинку (важи 1 сат):</p>" +
            $"<p><a href=\"{link}\">{link}</a></p>",
            ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetBySecurityTokenAsync(request.Token, ct)
            ?? throw new UnauthorizedAppException("Невалидан или истекао линк.");

        if (!user.IsSecurityTokenValid(request.Token))
        {
            throw new UnauthorizedAppException("Невалидан или истекао линк.");
        }

        user.SetPassword(_passwordHasher.Hash(request.NewPassword));
        await _users.SaveChangesAsync(ct);
    }
}
