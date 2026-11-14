
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class LifestyleController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Sinh hoạt & Thành tích";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Lifestyle";

            return View();
        }
    }
}
