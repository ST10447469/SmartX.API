namespace SmartX.API.Controllers;
using global::SmartX.API.Part_2;
using Microsoft.AspNetCore.Mvc;




[ApiController]
[Route("api/stream")]
public class StreamController(IngestionPipeline pipeline, DeviceRegistry registry,
                              CommandHistory commands, PredictionEngine prediction) : ControllerBase
{
    static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase) { "ON", "OFF", "OPEN", "CLOSE" };

    /// <summary>Devices POST telemetry here. Returns 202 plus which lane the packet took.</summary>
    [HttpPost("ingest")]
    public IActionResult Ingest([FromBody] StreamPacket packet)
    {
        if (string.IsNullOrWhiteSpace(packet.Mac) || string.IsNullOrWhiteSpace(packet.Metric))
            return BadRequest(new { error = "mac and metric are required." });
        var (lane, reason) = pipeline.Enqueue(packet);
        return Accepted(new { lane, reason });
    }

    [HttpGet("devices")]
    public IActionResult Devices(string? category, string? alertState, string? search, bool recordSearch = false)
    {
        if (recordSearch && !string.IsNullOrWhiteSpace(search))
            prediction.RecordEvent($"SEARCH:{search.Trim().ToLowerInvariant()}");
        return Ok(registry.Query(category, alertState, search));
    }

    [HttpGet("devices/{mac}/history")]
    public IActionResult History(string mac, int take = 100) => Ok(registry.History(mac.ToUpperInvariant(), take));

    [HttpGet("alerts")] public IActionResult Alerts() => Ok(registry.ActiveAlerts());
    [HttpGet("queues")] public IActionResult Queues() => Ok(pipeline.Snapshot());
    [HttpGet("insights")] public IActionResult Insights() => Ok(prediction.Suggest());

    [HttpPost("worker/pause")] public IActionResult Pause() { pipeline.Paused = true; return Ok(new { paused = true }); }
    [HttpPost("worker/resume")] public IActionResult Resume() { pipeline.Paused = false; return Ok(new { paused = false }); }

    [HttpGet("commands")] public IActionResult CommandLog() => Ok(commands.Snapshot());

    [HttpPost("commands")]
    public IActionResult Issue([FromBody] CommandRequest req)
    {
        if (!AllowedActions.Contains(req.Action ?? "")) return BadRequest(new { error = "Action must be ON, OFF, OPEN or CLOSE." });
        var cmd = commands.Issue(req.Mac ?? "", req.Action!);
        return cmd is null ? NotFound(new { error = "Unknown device, or the device is not an actuator." }) : Ok(cmd);
    }

    [HttpPost("commands/undo")]
    public IActionResult Undo()
    {
        var cmd = commands.Undo();
        return cmd is null ? NotFound(new { error = "Nothing to undo." }) : Ok(cmd);
    }
}
