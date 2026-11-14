
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class TImeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Thời gian";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Time";

            return View();
        }
    }
}
