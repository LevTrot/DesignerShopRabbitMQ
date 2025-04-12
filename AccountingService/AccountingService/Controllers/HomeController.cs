using Microsoft.AspNetCore.Mvc;

namespace AccountingService.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
