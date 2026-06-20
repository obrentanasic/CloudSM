using SmartMetering.Domain.Common;
using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetBySecurityTokenAsync(string token, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);

    /// <summary>All users — for the admin user-management panel.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    void Remove(User user);

    Task SaveChangesAsync(CancellationToken ct = default);
}
