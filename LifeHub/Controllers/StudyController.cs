
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class StudyController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Học tập";
            ViewData["ActivePage"] = "hoc-tap";

            return View();
        }
    }
}
