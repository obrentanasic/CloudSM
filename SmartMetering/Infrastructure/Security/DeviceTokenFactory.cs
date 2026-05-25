using System.Security.Cryptography;
using SmartMetering.Application.Abstractions;

namespace SmartMetering.Infrastructure.Security;

public sealed class DeviceTokenFactory : IDeviceTokenFactory
{
    public string Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
