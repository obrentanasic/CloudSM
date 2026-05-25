using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartMetering.Application;
using SmartMetering.Application.Authentication;
using SmartMetering.Infrastructure;
using SmartMetering.Infrastructure.Email;
using SmartMetering.Infrastructure.Persistence;
using SmartMetering.Infrastructure.Security;
using SmartMetering.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var sqlConnectionString = config.GetConnectionString("SqlDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'SqlDatabase'.");

var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var sendGridOptions = config.GetSection(SendGridOptions.SectionName).Get<SendGridOptions>() ?? new SendGridOptions();
var authLinks = config.GetSection("AuthLinks").Get<AuthLinkOptions>() ?? new AuthLinkOptions();

builder.Services.AddInfrastructure(sqlConnectionString, jwtOptions, sendGridOptions);
builder.Services.AddApplication();
builder.Services.AddSingleton(authLinks);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        };
    });

builder.Services.AddAuthorization();

const string corsPolicy = "SmartMeteringClient";
builder.Services.AddCors(options =>
    options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(authLinks.ClientBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token here (no 'Bearer ' prefix needed).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

var app = builder.Build();

var adminSeed = config.GetSection("AdminSeed").Get<UserSeedOptions>() ?? new UserSeedOptions();
await DatabaseSeeder.SeedAdminAsync(app.Services, adminSeed);

if (app.Environment.IsDevelopment())
{
    var consumerSeed = config.GetSection("ConsumerSeed").Get<UserSeedOptions>() ?? new UserSeedOptions();
    await DatabaseSeeder.SeedConsumerAsync(app.Services, consumerSeed);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
