namespace SmartX.API.Services;
using global::SmartX.API.Models;




/// <summary>
/// In-memory telemetry store. Demonstrates the "Advanced Arrays and Lists"
/// requirement: raw historical batches arrive as a jagged array (each
/// batch/poll-cycle can have a different number of readings), then get
/// flattened into optimised List&lt;T&gt; collections for querying.
/// </summary>
public class TelemetryStore
{
    private readonly List<TelemetryPacket<float>> _moistureHistory = new();
    private readonly List<TelemetryPacket<int>> _wattageHistory = new();
    private readonly List<TelemetryPacket<bool>> _valveHistory = new();

    public IReadOnlyList<TelemetryPacket<float>> MoistureHistory => _moistureHistory;
    public IReadOnlyList<TelemetryPacket<int>> WattageHistory => _wattageHistory;
    public IReadOnlyList<TelemetryPacket<bool>> ValveHistory => _valveHistory;

    public void AddMoisture(TelemetryPacket<float> packet) => _moistureHistory.Add(packet);
    public void AddWattage(TelemetryPacket<int> packet) => _wattageHistory.Add(packet);
    public void AddValve(TelemetryPacket<bool> packet) => _valveHistory.Add(packet);

    /// <summary>
    /// Seeds mock telemetry so the dashboard has data to demonstrate under
    /// load without physical ESP32 hardware attached.
    /// </summary>
    public void SeedMockData()
    {
        // Jagged array: each polling batch can hold a different number of raw
        // readings, exactly like real ESP32 devices reporting at uneven rates.
        float[][] rawMoistureBatches =
        {
            new float[] { 32.1f, 33.4f, 31.9f },
            new float[] { 30.2f, 29.8f },
            new float[] { 45.6f, 44.9f, 46.1f, 43.7f },
            new float[] { 28.4f },
            new float[] { 60.2f, 61.0f, 59.8f }, // simulated anomaly spike batch
            new float[] { 12.5f, 13.1f }          // simulated dry / disconnect-range batch
        };

        var rnd = new Random();

        for (int batch = 0; batch < rawMoistureBatches.Length; batch++)
        {
            foreach (var reading in rawMoistureBatches[batch])
            {
                _moistureHistory.Add(new TelemetryPacket<float>
                {
                    SensorId = $"MOIST-{batch:00}",
                    Value = reading,
                    Category = SensorCategory.Environmental,
                    Timestamp = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 120))
                });
            }
        }

        for (int i = 0; i < 40; i++)
        {
            _wattageHistory.Add(new TelemetryPacket<int>
            {
                SensorId = $"PWR-{i % 5:00}",
                Value = rnd.Next(50, 500),
                Category = SensorCategory.PowerConsumption,
                Timestamp = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 120))
            });
        }

        for (int i = 0; i < 20; i++)
        {
            _valveHistory.Add(new TelemetryPacket<bool>
            {
                SensorId = $"VALVE-{i % 4:00}",
                Value = rnd.Next(0, 2) == 1,
                Category = SensorCategory.Actuator,
                Timestamp = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 120))
            });
        }
    }
}
