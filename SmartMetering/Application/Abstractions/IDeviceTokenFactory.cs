namespace SmartMetering.Application.Abstractions;

public interface IDeviceTokenFactory
{
    /// <summary>Creates a cryptographically-random device access token.</summary>
    string Create();
}
