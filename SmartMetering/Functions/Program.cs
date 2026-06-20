using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SmartMetering.Application;
using SmartMetering.Infrastructure;
using SmartMetering.Infrastructure.Email;
using SmartMetering.Infrastructure.Payments;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var config = builder.Configuration;
var sqlConnectionString = config["SqlConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'SqlConnectionString'.");
var storageConnectionString = config["StorageConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'StorageConnectionString'.");

var sendGridOptions = new SendGridOptions
{
    ApiKey = config["SendGridApiKey"] ?? string.Empty,
    FromEmail = config["SendGridFromEmail"] ?? string.Empty,
    FromName = config["SendGridFromName"] ?? "Smart Metering",
};

var stripeOptions = new StripeOptions
{
    SecretKey = config["StripeSecretKey"] ?? string.Empty,
    WebhookSecret = config["StripeWebhookSecret"] ?? string.Empty,
};

builder.Services
    .AddPersistence(sqlConnectionString)
    .AddStorage(storageConnectionString)
    .AddSerialization()
    .AddEmail(sendGridOptions)
    .AddStripe(stripeOptions);
builder.Services.AddApplication();

builder.Build().Run();
