using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}