
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class AccountController : Controller
    {
        // Hiển thị trang đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Hiển thị trang đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}
