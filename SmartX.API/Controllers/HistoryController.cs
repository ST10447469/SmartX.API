using Microsoft.AspNetCore.Mvc;
using SmartX.API.Part3;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/history")]
public class HistoryController(TelemetryHistory history) : ControllerBase
{
    /// <summary>Which devices have history, how many readings, and the AVL tree height.</summary>
    [HttpGet]
    public IActionResult Summary() => Ok(history.Summary());

    /// <summary>Range query, e.g. /api/history/24:6F:28:AA:00:01?from=...&amp;to=...&amp;metric=moisture</summary>
    [HttpGet("{mac}")]
    public IActionResult Query(string mac, DateTimeOffset? from, DateTimeOffset? to, string? metric, int take = 200)
    {
        var f = from?.UtcDateTime ?? DateTime.MinValue;
        var t = to?.UtcDateTime ?? DateTime.MaxValue;
        if (t < f) return BadRequest(new { error = "'to' must be after 'from'." });
        return Ok(history.Query(mac, f, t, metric, Math.Clamp(take, 1, 1000)));
    }

    [HttpGet("{mac}/latest")]
    public IActionResult Latest(string mac, int take = 50) => Ok(history.Latest(mac, Math.Clamp(take, 1, 500)));
}