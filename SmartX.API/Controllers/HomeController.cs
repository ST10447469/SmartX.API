namespace SmartX.API.Controllers;
using Microsoft.AspNetCore.Mvc;



public class HomeController : Controller
{
    // Landing page: three architectural pillars (only ingestion enabled for Part 1).
    public IActionResult Index() => View();

    // The actual telemetry dashboard.
    public IActionResult Dashboard() => View();
}
