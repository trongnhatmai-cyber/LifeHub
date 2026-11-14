
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class TaskController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Công việc";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Task";

            return View();
        }
    }
}
