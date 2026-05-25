namespace SmartMetering.Application.Analytics;

public sealed record TelemetryPointDto(
    DateTime ObservationTime,
    double TotalEnergyKwh,
    double CurrentLoadKw,
    double? Voltage,
    int Tariff);

public sealed record MeterStatusDto(
    Guid MeterId,
    string SerialNumber,
    int ConnectionType,
    double LastTotalEnergyKwh,
    double LastLoadKw,
    double? LastVoltage,
    int CurrentTariff,
    DateTime LastHeartbeatUtc,
    bool IsOnline);
