
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class StudyController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Học tập";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Study";

            return View();
        }
    }
}
