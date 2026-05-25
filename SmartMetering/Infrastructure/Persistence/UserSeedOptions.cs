namespace SmartMetering.Infrastructure.Persistence;

/// <summary>Config-bound details for a user to seed on startup (used for the bootstrap admin and a dev consumer).</summary>
public sealed class UserSeedOptions
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
