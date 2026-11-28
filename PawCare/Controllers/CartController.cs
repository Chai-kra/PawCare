using PawCare.ViewModels;

namespace PawCare.Controllers;

public class CartController(Database db) : Controller
{
    private const string CartKey = "Cart";

    // GET /Cart
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
            else if (service != null)   // service: qty is always 1
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
        }

        ViewData["Total"] = total;
        return View(items);
    }

    // POST /Cart/Add
    [HttpPost]
    public IActionResult Add(string id, string? from)
    {
        // server side check: the id must exist in one of the two tables
        var product = db.Products.Find(id);
        var service = product == null ? db.Services.Find(id) : null;
        if (product == null && service == null)
        {
            TempData["Error"] = "That item does not exist.";
            return RedirectToAction("Products", "Home");
        }

        var lines = ReadCart();
        var line = lines.FirstOrDefault(l => l.Id == id);

        if (line == null)
        {
            lines.Add(new CartLine { Id = id, Qty = 1 });
        }
        else if (product != null)          // products can be added again
        {
            if (line.Qty + 1 > product.Stock)
            {
                TempData["Error"] = $"Only {product.Stock} left in stock.";
                return Back(from);
            }
            line.Qty += 1;
        }
        // service already in cart: keep qty 1, one booking is one slot

        SaveCart(lines);
        TempData["Success"] = (product != null ? product.Name : service!.Name) + " added to cart.";
        return Back(from);
    }

    // POST /Cart/Remove
    [HttpPost]
    public IActionResult Remove(string id)
    {
        var lines = ReadCart();
        lines.RemoveAll(l => l.Id == id);
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

    // POST /Cart/Checkout  (placeholder: the Cart view posts here, but there is no order table yet)
    [HttpPost]
    public IActionResult Checkout()
    {
        TempData["Error"] = "Checkout is not available yet.";
        return RedirectToAction("Index");
    }

    // ---- helpers ----
    private List<CartLine> ReadCart()
    {
        var text = HttpContext.Session.GetString(CartKey) ?? "";
        var list = new List<CartLine>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            list.Add(new CartLine { Id = bits[0], Qty = int.Parse(bits[1]) });
        }
        return list;
    }

    private void SaveCart(List<CartLine> lines)
    {
        var text = string.Join(",", lines.Select(l => $"{l.Id}:{l.Qty}"));
        HttpContext.Session.SetString(CartKey, text);
    }

    private IActionResult Back(string? from) =>
        from == "services"
            ? RedirectToAction("Services", "Home")
            : RedirectToAction("Products", "Home");
}

public class CartLine
{
    public string Id { get; set; } = "";
    public int Qty { get; set; }
}
