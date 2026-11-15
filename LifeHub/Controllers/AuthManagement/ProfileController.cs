
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Hồ sơ";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Profile";

            return View();
        }
    }
}
