using Microsoft.Extensions.DependencyInjection;
using SmartMetering.Application.Authentication;

namespace SmartMetering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
