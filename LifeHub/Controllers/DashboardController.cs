using System.Globalization;
using System.Security.Claims;
using LifeHub.Data.Mock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "bạn";
            var today = DateTime.Now.ToString("dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));

            var model = MockDashboardData.Build(userName, today);
            return View(model);
        }
    }
}
