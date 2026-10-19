
namespace SmartX.API.Controllers;

using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Dashboard() => View();

    public IActionResult CommandStream()
    {
        return View("~/Views/StreamPages/CommandStream.cshtml");
    }
}
