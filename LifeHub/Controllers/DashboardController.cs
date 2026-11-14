using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                UserName = User.FindFirstValue(ClaimTypes.Name) ?? "User",

                TodayDateFormatted = DateTime.Now.ToString(
                    "dddd, dd/MM/yyyy"
                ),

                // Sau này các số liệu này sẽ lấy từ Database
                PendingTasksCount = 4,
                ActiveProjectsCount = 3,
                CurrentGpa = 3.42m,
                TotalBalance = 12200000m,

                WeeklyMomentumPercent = 68,

                TodayTasks = new List<DashboardTaskItem>
            {
                new()
                {
                    Id = 1,
                    Title = "Hoàn thành bài tập ASP.NET",
                    Priority = "Cao",
                    Category = "Học tập",
                    DueTime = "Hôm nay",
                    IsCompleted = false
                }
            }
            };

            return View(model);
        }
    }
}