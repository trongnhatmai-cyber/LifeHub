using LifeHub.Models.MockData;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class HabitController : Controller
    {
        public IActionResult Index() => Page("HabitOverview", "HabitOverview");
        public IActionResult HabitList() => Page("HabitList", "HabitList");
        public IActionResult HabitTracking() => Page("HabitTracking", "HabitTracking");
        public IActionResult HabitStreak() => Page("HabitStreak", "HabitStreak");
        public IActionResult HabitStatistics() => Page("HabitStatistics", "HabitStatistics");

        private IActionResult Page(string page, string activePage)
        {
            var model = GoalHabitMockData.Get(page);
            ViewData["Title"] = model.Title;
            ViewData["ActivePage"] = activePage;
            return View(page == "HabitOverview" ? "Index" : page, model);
        }
    }
}
