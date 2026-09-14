namespace SmartX.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;


[ApiController]
[Route("api/meters")]
public class MetersController : ControllerBase
{
    // Operator overloading demo: aggregate two smart-meter loads.
    [HttpPost("aggregate")]
    public IActionResult Aggregate([FromBody] MeterAggregateRequest request)
    {
        var combined = request.A + request.B; // uses the overloaded + operator
        return Ok(combined);
    }
}