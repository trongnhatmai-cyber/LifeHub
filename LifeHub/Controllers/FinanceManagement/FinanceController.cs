
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class FinanceController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "tài chính";
            ViewData["ActivePage"] = "tai-chinh";

            return View();
        }
    }
}
