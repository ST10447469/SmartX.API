namespace SmartX.API.Models;



public class SmartMeterReading
{
    public string MeterId { get; set; } = string.Empty;
    public double WattageLoad { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static SmartMeterReading operator +(SmartMeterReading a, SmartMeterReading b) =>
        new()
        {
            MeterId = $"{a.MeterId}+{b.MeterId}",
            WattageLoad = a.WattageLoad + b.WattageLoad,
            Timestamp = DateTime.UtcNow
        };

    public static SmartMeterReading operator -(SmartMeterReading a, SmartMeterReading b) =>
        new()
        {
            MeterId = $"{a.MeterId}-{b.MeterId}",
            WattageLoad = a.WattageLoad - b.WattageLoad,
            Timestamp = DateTime.UtcNow
        };

    // Comparison operators must be overloaded in matching pairs (< with >, <= with >=).
    public static bool operator >(SmartMeterReading a, SmartMeterReading b) => a.WattageLoad > b.WattageLoad;
    public static bool operator <(SmartMeterReading a, SmartMeterReading b) => a.WattageLoad < b.WattageLoad;
    public static bool operator >=(SmartMeterReading a, SmartMeterReading b) => a.WattageLoad >= b.WattageLoad;
    public static bool operator <=(SmartMeterReading a, SmartMeterReading b) => a.WattageLoad <= b.WattageLoad;

    public override string ToString() => $"{MeterId}: {WattageLoad:F1} W";
}
