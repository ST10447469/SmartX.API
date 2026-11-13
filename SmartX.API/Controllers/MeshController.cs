using Microsoft.AspNetCore.Mvc;
using SmartX.API.Part3;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/mesh")]
public class MeshController(MeshGraph mesh) : ControllerBase
{
    /// <summary>Nodes, links, MST, down and cut-off nodes and reroutes in one response.</summary>
    [HttpGet("topology")]
    public IActionResult Topology() => Ok(mesh.Snapshot());

    /// <summary>BFS: everything currently connected to the gateway (or to ?from=NODE).</summary>
    [HttpGet("reachable")]
    public IActionResult Reachable(string? from)
    {
        var result = mesh.Reachable(from ?? MeshGraph.GatewayId);
        return result is null ? NotFound(new { error = "Unknown node." }) : Ok(result);
    }

    /// <summary>Fewest-hops route (BFS) and lowest-power route (MST) from a node to the gateway.</summary>
    [HttpGet("route/{id}")]
    public IActionResult Route(string id)
    {
        var result = mesh.Route(id);
        return result is null ? NotFound(new { error = "Unknown node." }) : Ok(result);
    }

    [HttpPost("nodes/{id}/down")]
    public IActionResult Down(string id) =>
        mesh.SetDown(id, true) ? Ok(mesh.Snapshot()) : BadRequest(new { error = "Unknown node, or the gateway cannot be taken down." });

    [HttpPost("nodes/{id}/up")]
    public IActionResult Up(string id) =>
        mesh.SetDown(id, false) ? Ok(mesh.Snapshot()) : BadRequest(new { error = "Unknown node." });

    [HttpPost("reset")]
    public IActionResult Reset() { mesh.Reset(); return Ok(mesh.Snapshot()); }
}