using System.Net.Http.Json;
using System.Text.Json;

// ── Smart Meter Simulator ───────────────────────────────────────────────────
// 1. Handshake: register this device with the platform using the serial number.
// 2. Loop: send a telemetry measurement every few seconds using the issued token.

var functionsBaseUrl = Environment.GetEnvironmentVariable("FUNCTIONS_BASE_URL") ?? "http://localhost:7071";
var intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_SECONDS"), out var s) ? s : 5;

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
using var http = new HttpClient { BaseAddress = new Uri(functionsBaseUrl) };

Console.WriteLine("=== Smart Meter Simulator ===");
Console.Write("Serial number (e.g. SM-2026-00001): ");
var serial = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();

Console.Write("Connection type [1=single-phase, 3=three-phase]: ");
var isThreePhase = (Console.ReadLine() ?? "1").Trim() == "3";
var connectionType = isThreePhase ? 1 : 0;

var deviceUuid = Guid.NewGuid().ToString();
Console.WriteLine($"Device UUID: {deviceUuid}");

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

Console.WriteLine($"Sending telemetry every {intervalSeconds}s. Press Ctrl+C to stop.");
while (true)
{
    var loadKw = Math.Round(random.NextDouble() * maxLoadKw, 3);
    totalEnergyKwh = Math.Round(totalEnergyKwh + loadKw * intervalSeconds / 3600.0, 3);

    // Occasionally simulate a voltage drop (anomaly) below 190V.
    double Voltage() => random.NextDouble() < 0.1 ? Math.Round(180 + random.NextDouble() * 8, 1) : Math.Round(225 + random.NextDouble() * 10, 1);
    double Current() => Math.Round(random.NextDouble() * 16, 2);
    double PowerFactor() => Math.Round(0.9 + random.NextDouble() * 0.09, 2);

    var measurement = new
    {
        totalEnergyKwh,
        currentLoadKw = loadKw,
        observationTime = DateTime.UtcNow,
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

    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
}

internal sealed record RegisterResponse(string DeviceAccessToken);
