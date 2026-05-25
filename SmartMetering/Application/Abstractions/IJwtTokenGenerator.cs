using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
