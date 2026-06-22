using System.Net.Http.Json;
using System.Text.Json;

// ── Smart Meter Simulator ───────────────────────────────────────────────────
// 1. Handshake: register this device with the platform using the serial number.
// 2. Loop: send a telemetry measurement every few seconds using the issued token.

var functionsBaseUrl = Environment.GetEnvironmentVariable("FUNCTIONS_BASE_URL") ?? "http://localhost:7071";
var intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_SECONDS"), out var s) ? s : 5;
var demoAnomaly = (Environment.GetEnvironmentVariable("DEMO_ANOMALY") ?? "none").Trim().ToLowerInvariant();
var demoHistory = bool.TryParse(Environment.GetEnvironmentVariable("DEMO_HISTORY"), out var seedHistory) && seedHistory;

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
using var http = new HttpClient { BaseAddress = new Uri(functionsBaseUrl) };

Console.WriteLine("=== Smart Meter Simulator ===");
if (demoAnomaly is "voltage" or "load" or "both")
{
    Console.WriteLine($"Demo anomaly enabled: {demoAnomaly} (forced on the second measurement).");
}
if (demoHistory)
{
    Console.WriteLine("Demo tariff history enabled: the first samples cover NT, VT and NT.");
}
Console.Write("Serial number (e.g. SM-2026-00001): ");
var serial = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();

Console.Write("Connection type [1=single-phase, 3=three-phase]: ");
var isThreePhase = (Console.ReadLine() ?? "1").Trim() == "3";
var connectionType = isThreePhase ? 1 : 0;

// A real meter's UUID is fixed in hardware, so persist it per serial and reuse it on every run.
// Otherwise each run looks like a different device trying to claim an already-paired meter (HTTP 409).
var uuidStorePath = Path.Combine(AppContext.BaseDirectory, $"device-{serial}.uuid");
string deviceUuid;
if (File.Exists(uuidStorePath))
{
    deviceUuid = File.ReadAllText(uuidStorePath).Trim();
    Console.WriteLine($"Device UUID (reused): {deviceUuid}");
}
else
{
    deviceUuid = Guid.NewGuid().ToString();
    File.WriteAllText(uuidStorePath, deviceUuid);
    Console.WriteLine($"Device UUID (new, saved): {deviceUuid}");
}

// ── 1. Handshake ─────────────────────────────────────────────────────────────
Console.WriteLine("Registering device (handshake)...");
string deviceToken;
try
{
    var registerResponse = await http.PostAsJsonAsync("/api/devices/register",
        new { serialNumber = serial, deviceUuid });

    if (!registerResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Registration failed ({(int)registerResponse.StatusCode}): {await registerResponse.Content.ReadAsStringAsync()}");
        return;
    }

    var payload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(jsonOptions);
    deviceToken = payload!.DeviceAccessToken;
    Console.WriteLine("Paired. Device access token received.");
}
catch (Exception ex)
{
    Console.WriteLine($"Could not reach the platform: {ex.Message}");
    return;
}

// ── 2. Telemetry loop ─────────────────────────────────────────────────────────
http.DefaultRequestHeaders.Add("X-Device-Token", deviceToken);
var random = new Random();
var totalEnergyKwh = Math.Round(random.NextDouble() * 500, 2); // starting cumulative reading
var maxLoadKw = isThreePhase ? 11.04 : 6.9;
var sampleIndex = 0;

Console.WriteLine($"Sending telemetry every {intervalSeconds}s. Press Ctrl+C to stop.");
while (true)
{
    var forceLoadSpike = sampleIndex == 1 && (demoAnomaly is "load" or "both");
    var forceVoltageDrop = sampleIndex == 1 && (demoAnomaly is "voltage" or "both");
    var loadKw = forceLoadSpike
        ? Math.Round(maxLoadKw * 1.25, 3)
        : Math.Round(random.NextDouble() * maxLoadKw, 3);
    totalEnergyKwh = Math.Round(totalEnergyKwh + loadKw * intervalSeconds / 3600.0, 3);

    double Voltage() => forceVoltageDrop
        ? 185.0
        : random.NextDouble() < 0.1
            ? Math.Round(180 + random.NextDouble() * 8, 1)
            : Math.Round(225 + random.NextDouble() * 10, 1);
    double Current() => Math.Round(random.NextDouble() * 16, 2);
    double PowerFactor() => Math.Round(0.9 + random.NextDouble() * 0.09, 2);

    var observationTime = demoHistory
        ? sampleIndex switch
        {
            0 => DateTime.UtcNow.Date.AddDays(-1).AddHours(4),
            1 => DateTime.UtcNow.Date.AddDays(-1).AddHours(10),
            2 => DateTime.UtcNow.Date.AddDays(-1).AddHours(22),
            _ => DateTime.UtcNow,
        }
        : DateTime.UtcNow;

    var measurement = new
    {
        totalEnergyKwh,
        currentLoadKw = loadKw,
        observationTime,
        voltageL1 = Voltage(),
        voltageL2 = isThreePhase ? Voltage() : (double?)null,
        voltageL3 = isThreePhase ? Voltage() : (double?)null,
        currentL1 = Current(),
        currentL2 = isThreePhase ? Current() : (double?)null,
        currentL3 = isThreePhase ? Current() : (double?)null,
        powerFactorL1 = PowerFactor(),
        powerFactorL2 = isThreePhase ? PowerFactor() : (double?)null,
        powerFactorL3 = isThreePhase ? PowerFactor() : (double?)null,
    };

    try
    {
        var response = await http.PostAsJsonAsync("/api/telemetry", measurement);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] load={loadKw}kW total={totalEnergyKwh}kWh -> {(int)response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Send failed: {ex.Message}");
    }

    sampleIndex++;
    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
}

internal sealed record RegisterResponse(string DeviceAccessToken);
