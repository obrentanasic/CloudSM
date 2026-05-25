using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SmartMetering.Infrastructure;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var config = builder.Configuration;
var sqlConnectionString = config["SqlConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'SqlConnectionString'.");
var storageConnectionString = config["StorageConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'StorageConnectionString'.");

builder.Services
    .AddPersistence(sqlConnectionString)
    .AddStorage(storageConnectionString)
    .AddSerialization();

builder.Build().Run();
