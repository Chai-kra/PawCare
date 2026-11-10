using Microsoft.AspNetCore.Mvc;

namespace PawCare.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Services() => View();

    public IActionResult About() => View();

    public IActionResult Contact() => View();
}
