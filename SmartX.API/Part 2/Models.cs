namespace SmartX.API.Part_2;


/// <summary>A telemetry packet exactly as a device (ESP32, smart plug...) would POST it.</summary>
public record StreamPacket
{
    public string Mac { get; init; } = "";
    public string? Name { get; init; }
    public string Category { get; init; } = "Environment";   // Hydroponics | Power | Environment
    public string DeviceType { get; init; } = "sensor";      // sensor | actuator
    public string Metric { get; init; } = "value";           // moisture | power | temperature ...
    public double Value { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class StreamDevice
{
    public string Mac { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string DeviceType { get; set; } = "sensor";
    public string State { get; set; } = "OFF";
    public string Metric { get; set; } = "";
    public double LastValue { get; set; }
    public DateTime LastSeen { get; set; }
}

public record DeviceView(string Mac, string Name, string Category, string DeviceType, string State,
    string Metric, double LastValue, DateTime LastSeen, bool Online, string[] Alerts);
public record DeviceCommand(Guid Id, string Mac, string DeviceName, string Action, string PreviousState, DateTime At);
public record Insight(string Kind, string Title, string Detail, string? Mac, string? Action, double Confidence);
public record ProcessedEntry(long Seq, string Lane, string Mac, string Metric, double Value, DateTime At);
public record HistoryPoint(DateTime T, double V);
public record CommandRequest(string Mac, string Action);

/// <summary>Thresholds that make a packet "critical" and therefore eligible for the priority queue.</summary>
public static class AlertRules
{
    public static bool IsCritical(StreamPacket p, out string reason)
    {
        var m = p.Metric.ToLowerInvariant();
        if (m == "moisture" && p.Value < 20) { reason = "Moisture below 20%"; return true; }
        if (m == "power" && p.Value > 3000) { reason = "Power spike above 3000 W"; return true; }
        if (m == "temperature" && p.Value > 45) { reason = "Temperature above 45 °C"; return true; }
        reason = "";
        return false;
    }

    /// <summary>Lower number = served first. Power spikes outrank everything else.</summary>
    public static int Severity(StreamPacket p) => p.Metric.Equals("power", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
}