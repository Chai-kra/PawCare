using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Models;

namespace PawCare.Controllers;

public class HomeController(Database db) : Controller
{
    public IActionResult Index() => View();

    public IActionResult Services(string? name)
    {
        name = name?.Trim() ?? "";
        var services = db.Services
            .Where (s => s.Name.Contains(name))
            .ToList();
        ViewData["ServiceName"] = name;
        return View(services);
    }

    public IActionResult About() => View();

    public IActionResult Contact() => View();
}
