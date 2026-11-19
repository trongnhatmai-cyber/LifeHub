using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.GoalManagement;
using LifeHub.Models.HabitManagement;
using LifeHub.Models.ViewModels.GoalManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class GoalController : Controller
    {
        private readonly ApplicationDbContext _context;
        public GoalController(ApplicationDbContext context) => _context = context;
        public Task<IActionResult> Index() => Page("GoalOverview", "GoalOverview");
        public Task<IActionResult> GoalList() => Page("GoalList", "GoalList");
        public Task<IActionResult> KeyResult() => Page("KeyResult", "KeyResult");
        public Task<IActionResult> GoalProgress() => Page("GoalProgress", "GoalProgress");
        public Task<IActionResult> GoalReview() => Page("GoalReview", "GoalReview");

        private async Task<IActionResult> Page(string page, string activePage)
        {
            var userId = User.GetUserId();
            var goals = await _context.Goals.AsNoTracking().Where(g => g.UserId == userId).OrderBy(g => g.TargetDate).ToListAsync();
            var goalIds = goals.Select(g => g.Id).ToArray();
            var results = await _context.KeyResults.AsNoTracking().Where(k => goalIds.Contains(k.GoalId)).ToListAsync();
            var model = new GoalHabitPageViewModel { Eyebrow="MỤC TIÊU", Title=page == "GoalOverview" ? "Mục tiêu của tôi" : page,
                Description="Theo dõi tiến độ mục tiêu cá nhân.", Accent="goal", SectionTitle="Mục tiêu", Items=goals.Select(g => new PageItemViewModel {
                    Title=g.Title, Subtitle=g.Description ?? "", Status=g.Status, Category=g.Category ?? g.GoalType, Value=$"{g.ProgressPercent}%",
                    Progress=Math.Clamp(g.ProgressPercent,0,100), DueDate=g.TargetDate?.ToString("dd/MM/yyyy") ?? "" }).ToList(),
                SecondaryTitle="Kết quả then chốt", SecondaryItems=results.Select(k => new PageItemViewModel { Title=k.Title,
                    Value=$"{k.CurrentValue ?? 0} / {k.TargetValue} {k.Unit}", Progress=k.TargetValue <= 0 ? 0 : Math.Clamp((int)((k.CurrentValue ?? 0) / k.TargetValue * 100),0,100), Status=k.IsCompleted ? "Hoàn thành" : "Đang thực hiện" }).ToList() };
            model.Metrics.Add(new PageMetricViewModel { Label="Tổng mục tiêu", Value=goals.Count.ToString() });
            model.Metrics.Add(new PageMetricViewModel { Label="Đang thực hiện", Value=goals.Count(g => !g.IsCompleted).ToString() });
            ViewData["Title"] = model.Title;
            ViewData["ActivePage"] = activePage;
            return View(page == "GoalOverview" ? "Index" : page, model);
        }
    }
}
