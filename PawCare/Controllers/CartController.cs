using Microsoft.AspNetCore.Mvc;
using PawCare.Models;
using PawCare.ViewModels;

namespace PawCare.Controllers;

public class CartController(Database db) : Controller
{
    private const string CartKey = "Cart";

    // GET /Cart  --show the list
    public IActionResult Index()
    {
        var lines = ReadCart();
        var items = new List<CartItemVM>();
        decimal total = 0;

        foreach (var line in lines)
        {
            var product = db.Products.Find(line.Id);
            var service = product == null ? db.Services.Find(line.Id) : null;

            if (product != null)
            {
                items.Add(new CartItemVM
                {
                    Id = product.Id,
                    Name = product.Name,
                    Type = "Product",
                    Price = product.Price,
                    Qty = line.Qty
                });
                total += product.Price * line.Qty;
            }
            else if (service != null)
            {
                items.Add(new CartItemVM
                {
                    Id = service.Id,
                    Name = service.Name,
                    Type = "Service",
                    Price = service.Price,
                    Qty = 1
                });
                total += service.Price;
            }
            // item deleted from the database: silently skipped
        }

        ViewData["Total"] = total;
        return View(items);
    }

    // POST /Cart/Add
    [HttpPost]
    public IActionResult Add(string id, string? from)
    {
        // server side check: the id must exist in Products or Services
        var product = db.Products.Find(id);
        var service = product == null ? db.Services.Find(id) : null;

        if (product == null && service == null)
        {
            TempData["Error"] = "That item is not available.";
            return Back(from);
        }

        var lines = ReadCart();
        var line = lines.FirstOrDefault(l => l.Id == id);

        if (line == null)
        {
            lines.Add(new CartLine { Id = id, Qty = 1 });
        }
        else if (product != null)
        {
            // stock check before increasing the quantity
            if (line.Qty + 1 > product.Stock)
            {
                TempData["Error"] = $"{product.Name}: only {product.Stock} left in stock.";
                return Back(from);
            }
            line.Qty += 1;
        }
        else
        {
            TempData["Error"] = $"{service!.Name} is already in your cart.";
            return Back(from);
        }

        SaveCart(lines);
        TempData["Success"] = $"{product?.Name ?? service!.Name} added to cart.";
        return Back(from);
    }

    // POST /Cart/Update
    [HttpPost]
    public IActionResult Update(string id, string op)
    {
        var lines = ReadCart();
        var line = lines.FirstOrDefault(l => l.Id == id);
        if (line == null) return RedirectToAction("Index");

        var product = db.Products.Find(id);

        switch (op)
        {
            case "plus":
                if (product == null)
                {
                    TempData["Error"] = "A service can only be booked once.";
                }
                else if (line.Qty + 1 > product.Stock)
                {
                    TempData["Error"] = $"{product.Name}: only {product.Stock} left in stock.";
                }
                else
                {
                    line.Qty += 1;
                }
                break;

            case "minus":
                line.Qty -= 1;
                if (line.Qty <= 0) lines.Remove(line);
                break;

            case "remove":
                lines.Remove(line);
                break;
        }

        SaveCart(lines);
        return RedirectToAction("Index");
    }

    // POST /Cart/Clear
    [HttpPost]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartKey);
        return RedirectToAction("Index");
    }

    // POST /Cart/Checkout
    // -- placeholder, the order/payment module is not wired yet
    [HttpPost]
    public IActionResult Checkout()
    {
        if (!ReadCart().Any())
        {
            TempData["Error"] = "Your cart is empty.";
        }
        else
        {
            TempData["Error"] = "Payment will do later....";
        }
        return RedirectToAction("Index");
    }

    // ---------- Session helpers ----------

    private List<CartLine> ReadCart()
    {
        var text = HttpContext.Session.GetString(CartKey) ?? "";
        var list = new List<CartLine>();

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            if (bits.Length != 2) continue;
            if (!int.TryParse(bits[1], out var qty) || qty < 1) continue;
            if (!db.Products.Any(p => p.Id == bits[0]) && !db.Services.Any(s => s.Id == bits[0]))
                continue;                      // id no longer exists, drop it
            list.Add(new CartLine { Id = bits[0], Qty = qty });
        }
        return list;
    }
    private void SaveCart(List<CartLine> lines)
    {
        var text = string.Join(",", lines.Select(l => $"{l.Id}:{l.Qty}"));
        HttpContext.Session.SetString(CartKey, text);
    }

    // send the user back to the page they clicked from
    private IActionResult Back(string? from) =>
        from == "services"
            ? RedirectToAction("Services", "Home")
            : RedirectToAction("Products", "Home");
}