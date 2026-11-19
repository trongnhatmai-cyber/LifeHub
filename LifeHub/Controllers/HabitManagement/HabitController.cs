using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.GoalManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class HabitController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HabitController(ApplicationDbContext context) => _context = context;
        public Task<IActionResult> Index() => Page("HabitOverview", "HabitOverview");
        public Task<IActionResult> HabitList() => Page("HabitList", "HabitList");
        public Task<IActionResult> HabitTracking() => Page("HabitTracking", "HabitTracking");
        public Task<IActionResult> HabitStreak() => Page("HabitStreak", "HabitStreak");
        public Task<IActionResult> HabitStatistics() => Page("HabitStatistics", "HabitStatistics");

        private async Task<IActionResult> Page(string page, string activePage)
        {
            var userId = User.GetUserId();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var habits = await _context.Habits.AsNoTracking().Where(h => h.UserId == userId && h.Active).OrderBy(h => h.Title).ToListAsync();
            var ids = habits.Select(h => h.Id).ToArray();
            var weekStart = today.AddDays(-6);
            var logs = await _context.HabitLogs.AsNoTracking().Where(l => ids.Contains(l.HabitId) && l.CheckDate >= weekStart && l.CheckDate <= today).ToListAsync();
            var model = new GoalHabitPageViewModel { Eyebrow="THÓI QUEN", Title="Thói quen của tôi", Description="Theo dõi thói quen cá nhân.",
                Accent="habit", SectionTitle="Thói quen", Items=habits.Select(h => new PageItemViewModel { Title=h.Title,
                    Subtitle=h.Frequency, Status=logs.Any(l => l.HabitId == h.Id && l.IsCompleted) ? "Hoàn thành hôm nay" : "Chưa hoàn thành",
                    Category=h.Frequency, Value=$"{h.CurrentStreak} ngày", Progress=h.TargetPerPeriod > 0 ? Math.Min(100, h.CurrentStreak * 100 / h.TargetPerPeriod) : 0 }).ToList() };
            model.Metrics.Add(new PageMetricViewModel { Label="Thói quen đang bật", Value=habits.Count.ToString() });
            model.Metrics.Add(new PageMetricViewModel { Label="Hoàn thành hôm nay", Value=logs.Count(l => l.CheckDate == today && l.IsCompleted).ToString() });
            model.Chart = Enumerable.Range(0, 7).Select(offset => {
                var date = weekStart.AddDays(offset);
                var completed = logs.Count(l => l.CheckDate == date && l.IsCompleted);
                return new ChartPointViewModel { Label=date.ToString("dd/MM"), Value=habits.Count == 0 ? 0 : (int)Math.Round(completed * 100d / habits.Count) };
            }).ToList();
            ViewData["Title"] = model.Title;
            ViewData["ActivePage"] = activePage;
            return View(page == "HabitOverview" ? "Index" : page, model);
        }
    }
}
