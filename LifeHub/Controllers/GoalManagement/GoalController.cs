
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class GoalController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Mục tiêu";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Goal";

            return View();
        }
    }
}