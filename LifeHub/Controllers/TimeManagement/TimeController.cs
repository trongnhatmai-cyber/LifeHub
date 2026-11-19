
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.TimeManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class TimeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TimeController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Thời gian";
            ViewData["ActivePage"] = "Time";
            var userId = User.GetUserId();
            var today = DateTime.Today;
            var events = await _context.Events.AsNoTracking().Where(e => e.UserId == userId)
                .OrderBy(e => e.StartTime).Select(e => new TimeCalendarEvent { Id=e.Id, Title=e.Title,
                    StartTime=e.StartTime, EndTime=e.EndTime.ToDateTime(TimeOnly.MinValue), Location=e.Location ?? "",
                    EventType=e.EventType, IsAllDay=e.IsAllDay }).ToListAsync();
            var sessions = await _context.PomodoroSessions.AsNoTracking().Where(p => p.UserId == userId && p.IsCompleted && p.CompletedAt != null)
                .OrderByDescending(p => p.CompletedAt).Take(30).Select(p => new PomodoroSessionHistory { Id=p.Id,
                    DurationMinutes=p.DurationMinutes, CompletedAt=p.CompletedAt!.Value, IsCompleted=p.IsCompleted,
                    TaskOrCourseName=p.TaskId.HasValue ? "Công việc" : p.CourseId.HasValue ? "Khóa học" : "Tập trung" }).ToListAsync();
            return View("~/Views/Time/Index.cshtml", new TimeViewModel { Events=events, PomodoroHistory=sessions,
                TodayEventsCount=events.Count(e => e.StartTime.Date == today), PomodoroCompletedCount=sessions.Count,
                TotalFocusMinutes=sessions.Sum(p => p.DurationMinutes), UpcomingDeadlinesCount=events.Count(e => e.StartTime >= today) });
        }
    }
}
