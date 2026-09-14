namespace SmartX.API.Models;


/// <summary>
/// Generic wrapper that lets the ingestion pipeline handle disparate telemetry
/// types (float moisture readings, int wattage readings, bool valve states)
/// through one uniform shape, without boxing value types into `object`.
/// </summary>
public class TelemetryPacket<T>
{
    public string SensorId { get; set; } = string.Empty;
    public T Value { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public SensorCategory Category { get; set; }

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] {SensorId} ({Category}) = {Value}";
}