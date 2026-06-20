using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Application.Payments;
using SmartMetering.Infrastructure.Email;
using SmartMetering.Infrastructure.Payments;
using SmartMetering.Infrastructure.Persistence;
using SmartMetering.Infrastructure.Persistence.Repositories;
using SmartMetering.Infrastructure.Security;
using SmartMetering.Infrastructure.Serialization;
using SmartMetering.Infrastructure.Storage;

namespace SmartMetering.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Azure SQL via EF Core + relational repositories + device token factory.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string sqlConnectionString)
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
        services.AddScoped<IConsumptionLimitRepository, ConsumptionLimitRepository>();
        services.AddScoped<ITariffModelRepository, TariffModelRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IManualReadingRepository, ManualReadingRepository>();
        services.AddSingleton<IDeviceTokenFactory, DeviceTokenFactory>();

        return services;
    }

    /// <summary>Azure Table Storage repositories + the telemetry queue sender.</summary>
    public static IServiceCollection AddStorage(this IServiceCollection services, string storageConnectionString)
    {
        services.AddSingleton(new StorageOptions(storageConnectionString));
        services.AddScoped<ITelemetryRepository, TelemetryTableRepository>();
        services.AddScoped<IMeterStatusRepository, MeterStatusTableRepository>();
        services.AddScoped<IAlertLogRepository, AlertLogTableRepository>();
        services.AddSingleton<ITelemetryQueue, TelemetryQueue>();
        services.AddSingleton<IMeterStatusQueue, MeterStatusQueue>();
        services.AddSingleton<IAlertQueue, AlertQueue>();
        services.AddSingleton<IInvoiceDocumentStorage, InvoiceBlobStorage>();
        services.AddSingleton<IImageStorage, MeterReadingImageStorage>();
        return services;
    }

    public static IServiceCollection AddSerialization(this IServiceCollection services)
    {
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();
        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, JwtOptions jwtOptions)
    {
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        return services;
    }

    public static IServiceCollection AddEmail(this IServiceCollection services, SendGridOptions sendGridOptions)
    {
        services.AddSingleton(sendGridOptions);
        services.AddSingleton<IEmailService, SendGridEmailService>();
        return services;
    }

    public static IServiceCollection AddStripe(this IServiceCollection services, StripeOptions stripeOptions)
    {
        services.AddSingleton(stripeOptions);
        services.AddSingleton<IStripeGateway, StripeGateway>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}