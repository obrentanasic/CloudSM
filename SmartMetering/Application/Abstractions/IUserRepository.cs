using SmartMetering.Domain.Common;
using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetBySecurityTokenAsync(string token, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
