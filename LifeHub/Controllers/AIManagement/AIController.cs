
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class AIController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "AI";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "AI";

            return View();
        }
    }
}
