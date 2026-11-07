using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Page1()
    {
        return View();
    }

    public IActionResult Page2()
    {
        return View();
    }
}
