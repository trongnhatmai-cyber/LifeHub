
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

            // Hồ sơ đã chuyển vào Cài đặt > Tài khoản cá nhân; giữ đường dẫn cũ /Profile để link cũ vẫn dùng được.
            return RedirectToAction("Profile", "Settings");
        }
    }
}
