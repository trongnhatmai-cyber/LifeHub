using LifeHub.Models.MockData;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class GoalController : Controller
    {
        public IActionResult Index() => Page("GoalOverview", "GoalOverview");
        public IActionResult GoalList() => Page("GoalList", "GoalList");
        public IActionResult KeyResult() => Page("KeyResult", "KeyResult");
        public IActionResult GoalProgress() => Page("GoalProgress", "GoalProgress");
        public IActionResult GoalReview() => Page("GoalReview", "GoalReview");

        private IActionResult Page(string page, string activePage)
        {
            var model = GoalHabitMockData.Get(page);
            ViewData["Title"] = model.Title;
            ViewData["ActivePage"] = activePage;
            return View(page == "GoalOverview" ? "Index" : page, model);
        }
    }
}
