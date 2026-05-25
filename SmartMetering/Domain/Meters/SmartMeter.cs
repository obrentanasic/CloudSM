using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Meters;

/// <summary>
/// A physical smart meter registered under a <see cref="Properties.Property"/>.
/// Lifecycle: registered (Unpaired) → handshake binds a device UUID → Paired with an access token.
/// </summary>
public class SmartMeter : AggregateRoot
{
    // Fixed maximum approved power per connection type (kW), per project spec.
    public const decimal SinglePhaseMaxKw = 6.9m;
    public const decimal ThreePhaseMaxKw = 11.04m;

    private SmartMeter()
    {
        SerialNumber = string.Empty;
    }

    private SmartMeter(string serialNumber, ConnectionType connectionType, string? note, EntityId propertyId)
    {
        SerialNumber = serialNumber;
        ConnectionType = connectionType;
        MaxApprovedPowerKw = PowerFor(connectionType);
        Note = note;
        PropertyId = propertyId;
        PairingStatus = PairingStatus.Unpaired;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string SerialNumber { get; private set; }

    public ConnectionType ConnectionType { get; private set; }

    public decimal MaxApprovedPowerKw { get; private set; }

    public string? Note { get; private set; }

    public EntityId PropertyId { get; private set; }

    public PairingStatus PairingStatus { get; private set; }

    public string? DeviceUuid { get; private set; }

    public string? DeviceAccessToken { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static decimal PowerFor(ConnectionType type) =>
        type == ConnectionType.ThreePhase ? ThreePhaseMaxKw : SinglePhaseMaxKw;

    /// <summary>Step 1 of pairing: consumer registers the meter by its printed serial number.</summary>
    public static SmartMeter Register(string serialNumber, ConnectionType connectionType, string? note, EntityId propertyId) =>
        new(serialNumber.Trim().ToUpperInvariant(), connectionType, note?.Trim(), propertyId);

    public void UpdateDetails(ConnectionType connectionType, string? note)
    {
        ConnectionType = connectionType;
        MaxApprovedPowerKw = PowerFor(connectionType);
        Note = note?.Trim();
    }

    /// <summary>Steps 2–3 of pairing: binds the device UUID and stores the issued access token.</summary>
    public void CompletePairing(string deviceUuid, string deviceAccessToken)
    {
        DeviceUuid = deviceUuid;
        DeviceAccessToken = deviceAccessToken;
        PairingStatus = PairingStatus.Paired;
    }
}
