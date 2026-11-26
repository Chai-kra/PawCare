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

    public IActionResult Products(string? categoryId, decimal? minPrice, decimal? maxPrice)
    {
        var q = db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrEmpty(categoryId))
            q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue)
            q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            q = q.Where(p => p.Price <= maxPrice.Value);
        ViewData["CategoryId"] = categoryId;
        ViewData["MinPrice"] = minPrice;
        ViewData["MaxPrice"] = maxPrice;
        ViewBag.Categories = db.Categories
                               .Where(c => c.Products.Any())
                               .OrderBy(c => c.Id)
                               .ToList();

        return View(q.OrderBy(p => p.Price).ToList());
    }
}
