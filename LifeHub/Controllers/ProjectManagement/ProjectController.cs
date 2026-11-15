
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class ProjectController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Dự án";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Project";

            return View();
        }
    }
}
