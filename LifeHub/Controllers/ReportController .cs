using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class ReportController : Controller
    {
        // TODO(backend): thay bằng dữ liệu thật; giữ nguyên tên controller và action để link không đổi.
        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Study() => View();

        [HttpGet]
        public IActionResult Tasks() => View();

        [HttpGet]
        public IActionResult Finance() => View();

        [HttpGet]
        public IActionResult Productivity() => View();
    }
}