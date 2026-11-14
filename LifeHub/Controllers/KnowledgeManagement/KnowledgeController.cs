
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class KnowledgeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Kiến thức & Tài liệu";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Knowledge";

            return View();
        }
    }
}