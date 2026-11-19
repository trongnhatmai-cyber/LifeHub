using System.Globalization;
using System.Security.Claims;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            var tomorrow = todayDate.AddDays(1);
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "bạn";
            var today = DateTime.Now.ToString("dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
            var myTasks = _context.Tasks.AsNoTracking().Where(t => t.UserId == userId);
            var tasks = await myTasks.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == DateTime.Today).OrderBy(t => t.Deadline).Take(8).ToListAsync();
            var events = await _context.Events.AsNoTracking().Where(e => e.UserId == userId && e.StartTime >= DateTime.Today).OrderBy(e => e.StartTime).Take(5).ToListAsync();
            var goals = await _context.Goals.AsNoTracking().Where(g => g.UserId == userId && !g.IsCompleted).OrderByDescending(g => g.ProgressPercent).Take(4).ToListAsync();
            var habits = await _context.Habits.AsNoTracking().Where(h => h.UserId == userId && h.Active).OrderBy(h => h.Title).Take(6).ToListAsync();
            var habitIds = habits.Select(h => h.Id).ToArray();
            var completedHabitIds = await _context.HabitLogs.AsNoTracking().Where(h => habitIds.Contains(h.HabitId) && h.CheckDate == todayDate && h.IsCompleted).Select(h => h.HabitId).ToListAsync();
            var model = new DashboardViewModel { UserName=userName, TodayDateFormatted=today, PendingTasksCount=await myTasks.CountAsync(t => t.Status != "completed"),
                ActiveProjectsCount=await _context.ProjectMembers.AsNoTracking().CountAsync(pm => pm.UserId == userId),
                TotalBalance=await _context.Wallets.AsNoTracking().Where(w => w.UserId == userId && w.IsActive).SumAsync(w => (decimal?)w.InitialBalance) ?? 0m,
                TodayTasks=tasks.Select(t => new DashboardTaskItem { Id=t.Id, Title=t.Title, Priority=t.Priority, Category=t.ProjectId.HasValue ? "Dự án" : "Cá nhân", DueTime=t.Deadline?.ToString("HH:mm") ?? "", IsCompleted=t.Status == "completed" }).ToList(),
                UpcomingEvents=events.Select(e => new DashboardEventItem { Id=e.Id, Title=e.Title, Time=e.StartTime.ToString("HH:mm"), Location=e.Location ?? "", Tag=e.EventType }).ToList(),
                Goals=goals.Select(g => new DashboardGoalItem { Id=g.Id, Title=g.Title, ProgressPercent=g.ProgressPercent, Category=g.Category ?? g.GoalType }).ToList(),
                Habits=habits.Select(h => new DashboardHabitItem { Id=h.Id, Title=h.Title, StreakDays=h.CurrentStreak, IsCompletedToday=completedHabitIds.Contains(h.Id) }).ToList() };
            return View(model);
        }
    }
}
