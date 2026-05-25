namespace SmartMetering.Infrastructure.Persistence;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "System";

    public string LastName { get; set; } = "Administrator";

    public string PhoneNumber { get; set; } = string.Empty;
}
