using Microsoft.AspNetCore.Mvc;

namespace PawCare.Controller
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
