using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Infrastructure.Email;
using SmartMetering.Infrastructure.Persistence;
using SmartMetering.Infrastructure.Persistence.Repositories;
using SmartMetering.Infrastructure.Security;
using SmartMetering.Infrastructure.Serialization;

namespace SmartMetering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqlConnectionString,
        JwtOptions jwtOptions,
        SendGridOptions sendGridOptions)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                sqlConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 6,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ISmartMeterRepository, SmartMeterRepository>();

        services.AddSingleton(jwtOptions);
        services.AddSingleton(sendGridOptions);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IEmailService, SendGridEmailService>();
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        return services;
    }
}
