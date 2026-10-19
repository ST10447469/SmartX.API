using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers;

/// <summary>Serves the Part 2 pages. The data comes from the StreamController API.</summary>
public class StreamPagesController : Controller
{
    [HttpGet("command-stream")]
    public IActionResult CommandStream() => View();

    [HttpGet("topology")]
    public IActionResult Topology() => View();
}