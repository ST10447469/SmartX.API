namespace SmartX.API.Controllers;
using SmartX.API.Services;
using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;


[ApiController]
[Route("api/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryStore _store;

    public TelemetryController(TelemetryStore store)
    {
        _store = store;
    }

    // One endpoint per concrete TelemetryPacket<T> — demonstrates generics
    // resolving to different closed types (float / int / bool) at compile time.
    [HttpPost("moisture")]
    public IActionResult IngestMoisture([FromBody] TelemetryPacket<float> packet)
    {
        _store.AddMoisture(packet);
        return Ok(packet);
    }

    [HttpPost("wattage")]
    public IActionResult IngestWattage([FromBody] TelemetryPacket<int> packet)
    {
        _store.AddWattage(packet);
        return Ok(packet);
    }

    [HttpPost("valve")]
    public IActionResult IngestValve([FromBody] TelemetryPacket<bool> packet)
    {
        _store.AddValve(packet);
        return Ok(packet);
    }

    // Live feed powering the dashboard's engagement feature (anomaly highlighting).
    [HttpGet("feed")]
    public IActionResult GetFeed()
    {
        const float lowThreshold = 25f;
        const float highThreshold = 55f;

        var feed = _store.MoistureHistory
            .OrderByDescending(p => p.Timestamp)
            .Take(15)
            .Select(p => new
            {
                p.SensorId,
                p.Value,
                p.Timestamp,
                IsAnomaly = p.Value < lowThreshold || p.Value > highThreshold
            });

        return Ok(feed);
    }
}
