using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers;

public class StreamPagesController : Controller
{
    [HttpGet("command-stream")]
    public IActionResult CommandStream() => View();

    [HttpGet("topology")]
    public IActionResult Topology() => View();
}