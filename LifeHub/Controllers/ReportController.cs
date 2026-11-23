using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public Task<IActionResult> Index(string? period = "month") => BuildReportAsync("overview", period);

    [HttpGet]
    public Task<IActionResult> Study(string? period = "month") => BuildReportAsync("study", period);

    [HttpGet]
    public Task<IActionResult> Tasks(string? period = "month") => BuildReportAsync("tasks", period);

    [HttpGet]
    public Task<IActionResult> Finance(string? period = "month") => BuildReportAsync("finance", period);

    [HttpGet]
    public Task<IActionResult> Productivity(string? period = "month") => BuildReportAsync("productivity", period);

    private async Task<IActionResult> BuildReportAsync(string section, string? period)
    {
        ViewData["Title"] = "Thống kê";
        ViewData["ActivePage"] = "Report";

        var userId = User.GetUserId();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var periodKey = NormalizePeriod(period);
        var fromDate = periodKey switch
        {
            "week" => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),
            "year" => new DateOnly(today.Year, 1, 1),
            "all" => (DateOnly?)null,
            _ => new DateOnly(today.Year, today.Month, 1)
        };
        var rangeStart = fromDate?.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var periodLabel = periodKey switch
        {
            "week" => "Tuần này",
            "year" => "Năm nay",
            "all" => "Toàn thời gian",
            _ => "Tháng này"
        };

        var courses = await _context.Courses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync();
        var courseIds = courses.Select(c => c.Id).ToArray();
        var assignments = await _context.Assignments.AsNoTracking().Where(a => courseIds.Contains(a.CourseId)).ToListAsync();
        decimal? ScoreFor(int courseId)
        {
            var course = courses.First(c => c.Id == courseId);
            if (course.FinalScore.HasValue) return course.FinalScore.Value;
            var scored = assignments.Where(a => a.CourseId == courseId && a.Score.HasValue).ToList();
            if (scored.Count == 0) return null;
            decimal Normalize(LifeHub.Models.StudyManagement.Assignment assignment)
            {
                var maxScore = assignment.MaxScore.GetValueOrDefault(10m);
                return maxScore <= 0 ? 0 : Math.Clamp(assignment.Score!.Value / maxScore * 10m, 0m, 10m);
            }
            var weighted = scored.Where(a => a.WeightPercent > 0).ToList();
            var weight = weighted.Sum(a => a.WeightPercent);
            return weight > 0
                ? weighted.Sum(a => Normalize(a) * a.WeightPercent) / weight
                : scored.Average(Normalize);
        }
        var courseScores = courses.Select(c => ScoreFor(c.Id)).Where(score => score.HasValue).Select(score => score!.Value).ToList();

        var taskQuery = _context.Tasks.AsNoTracking().Where(t => t.UserId == userId || t.AssignedTo == userId);
        if (rangeStart.HasValue) taskQuery = taskQuery.Where(t => (t.UpdatedAt ?? t.CreatedAt) >= rangeStart.Value && (t.UpdatedAt ?? t.CreatedAt) < rangeEnd);
        var tasks = await taskQuery.ToListAsync();

        var transactionQuery = _context.Transactions.AsNoTracking().Where(t => t.UserId == userId && t.TransactionDate <= today);
        if (fromDate.HasValue) transactionQuery = transactionQuery.Where(t => t.TransactionDate >= fromDate.Value);
        var transactions = await transactionQuery.ToListAsync();
        var wallets = await _context.Wallets.AsNoTracking().Where(w => w.UserId == userId && w.IsActive).ToListAsync();

        var goalQuery = _context.Goals.AsNoTracking().Where(g => g.UserId == userId);
        if (rangeStart.HasValue) goalQuery = goalQuery.Where(g => g.CreatedAt >= rangeStart.Value && g.CreatedAt < rangeEnd);
        var goals = await goalQuery.ToListAsync();

        var habits = await _context.Habits.AsNoTracking().Where(h => h.UserId == userId && h.Active).ToListAsync();
        var habitIds = habits.Select(h => h.Id).ToArray();
        var habitLogsQuery = _context.HabitLogs.AsNoTracking().Where(log => habitIds.Contains(log.HabitId) && log.CheckDate <= today);
        if (fromDate.HasValue) habitLogsQuery = habitLogsQuery.Where(log => log.CheckDate >= fromDate.Value);
        var habitLogs = await habitLogsQuery.ToListAsync();

        var focusQuery = _context.PomodoroSessions.AsNoTracking().Where(session => session.UserId == userId && session.IsCompleted && session.StartedAt < rangeEnd);
        if (rangeStart.HasValue) focusQuery = focusQuery.Where(session => session.StartedAt >= rangeStart.Value);
        var focusMinutes = await focusQuery.SumAsync(session => (int?)session.DurationMinutes) ?? 0;

        var income = transactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount);
        var expense = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
        var completedTasks = tasks.Count(t => t.Status == "done");
        var openTasks = tasks.Count - completedTasks;
        var overdueTasks = tasks.Count(t => t.Status != "done" && t.Deadline.HasValue && t.Deadline.Value.Date < DateTime.Today);
        var gradedAssignments = assignments.Count(a => a.Score.HasValue);
        var completedHabitLogs = habitLogs.Count(log => log.IsCompleted);

        return View("~/Views/Report/Index.cshtml", new ReportPageViewModel
        {
            Section = section,
            Period = periodKey,
            PeriodLabel = periodLabel,
            ActionName = section switch { "study" => nameof(Study), "tasks" => nameof(Tasks), "finance" => nameof(Finance), "productivity" => nameof(Productivity), _ => nameof(Index) },
            FromDate = fromDate,
            ToDate = today,
            CourseCount = courses.Count,
            CompletedCourseCount = courses.Count(c => c.Status == "completed"),
            GradedAssignmentCount = gradedAssignments,
            AverageCourseScore = courseScores.Count == 0 ? null : courseScores.Average(),
            TaskCount = tasks.Count,
            CompletedTaskCount = completedTasks,
            OpenTaskCount = openTasks,
            OverdueTaskCount = overdueTasks,
            TaskCompletionPercent = tasks.Count == 0 ? 0 : (int)Math.Round(completedTasks * 100d / tasks.Count),
            Income = income,
            Expense = expense,
            WalletBalance = wallets.Sum(wallet => wallet.InitialBalance),
            TransactionCount = transactions.Count,
            ActiveGoalCount = goals.Count(goal => !goal.IsCompleted && goal.Status != "completed"),
            CompletedGoalCount = goals.Count(goal => goal.IsCompleted || goal.Status == "completed"),
            ActiveHabitCount = habits.Count,
            CompletedHabitLogCount = completedHabitLogs,
            HabitLogCount = habitLogs.Count,
            FocusMinutes = focusMinutes
        });
    }

    private static string NormalizePeriod(string? period) => period?.Trim().ToLowerInvariant() switch
    {
        "week" => "week",
        "year" => "year",
        "all" => "all",
        _ => "month"
    };
}
