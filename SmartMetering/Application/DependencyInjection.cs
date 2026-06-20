using Microsoft.Extensions.DependencyInjection;
using SmartMetering.Application.Analytics;
using SmartMetering.Application.Authentication;
using SmartMetering.Application.Billing;
using SmartMetering.Application.Limits;
using SmartMetering.Application.ManualReadings;
using SmartMetering.Application.Meters;
using SmartMetering.Application.Network;
using SmartMetering.Application.Properties;

namespace SmartMetering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ILimitService, LimitService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IManualReadingService, ManualReadingService>();
        services.AddScoped<INetworkOverviewService, NetworkOverviewService>();
        return services;
    }
}
