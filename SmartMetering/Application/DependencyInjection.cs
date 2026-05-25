using Microsoft.Extensions.DependencyInjection;
using SmartMetering.Application.Authentication;
using SmartMetering.Application.Meters;
using SmartMetering.Application.Properties;

namespace SmartMetering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IMeterService, MeterService>();
        return services;
    }
}
