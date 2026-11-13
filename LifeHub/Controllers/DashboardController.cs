using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Tổng quan";
            ViewData["ActivePage"] = "Dashboard";
            return View();
        }
    }
}