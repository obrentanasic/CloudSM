using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Users;

public class User : AggregateRoot
{
    // Required by EF Core.
    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    private User(string firstName, string lastName, string email, string phoneNumber, UserRole role)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        Role = role;
        Status = UserStatus.PendingActivation;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string PhoneNumber { get; private set; }

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; }

    public string? PasswordHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Single-use, time-limited token used for both account activation and password reset.
    public string? SecurityToken { get; private set; }

    public DateTime? SecurityTokenExpiresAtUtc { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static User Create(string firstName, string lastName, string email, string phoneNumber, UserRole role) =>
        new(firstName.Trim(), lastName.Trim(), email.Trim().ToLowerInvariant(), phoneNumber.Trim(), role);

    /// <summary>Generates a new single-use security token and returns its raw value.</summary>
    public string IssueSecurityToken(TimeSpan validFor)
    {
        SecurityToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        SecurityTokenExpiresAtUtc = DateTime.UtcNow.Add(validFor);
        return SecurityToken;
    }

    public bool IsSecurityTokenValid(string token) =>
        !string.IsNullOrEmpty(SecurityToken)
        && SecurityToken == token
        && SecurityTokenExpiresAtUtc is { } expiry
        && expiry > DateTime.UtcNow;

    /// <summary>Sets the password hash, activates the account, and clears the security token.</summary>
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        SecurityToken = null;
        SecurityTokenExpiresAtUtc = null;
    }

    public bool CanLogin => Status == UserStatus.Active && !string.IsNullOrEmpty(PasswordHash);

    public void Suspend() => Status = UserStatus.Suspended;

    /// <summary>
    /// Lifts a suspension. A user who had already set a password returns to Active; one who never
    /// activated returns to PendingActivation (so the activation link still governs first login).
    /// </summary>
    public void Reactivate() =>
        Status = string.IsNullOrEmpty(PasswordHash) ? UserStatus.PendingActivation : UserStatus.Active;
}
