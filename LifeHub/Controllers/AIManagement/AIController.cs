using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.AIManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskEntity = LifeHub.Models.TaskManagement.Task;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Trợ lý AI cá nhân: tổng hợp dữ liệu thật từ các phân hệ của người dùng,
    /// gợi ý ưu tiên, lập kế hoạch tập trung và tạo nhanh công việc.
    /// </summary>
    [Authorize]
    public class AIController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AIController(ApplicationDbContext context) => _context = context;

        // GET /AI/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Trợ lý AI";
            ViewData["ActivePage"] = "AI";

            var uid = User.GetUserId();
            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var weekAhead = today.AddDays(7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var openTasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == uid && t.Status != "done" && t.Status != "completed")
                .OrderBy(t => t.Deadline == null).ThenBy(t => t.Deadline)
                .ToListAsync();

            var userCourseIds = _context.Courses.AsNoTracking()
                .Where(c => c.UserId == uid)
                .Select(c => c.Id);

            var upcomingAssignments = await _context.Assignments.AsNoTracking()
                .Where(a => userCourseIds.Contains(a.CourseId) && !a.IsSubmitted && a.DueDate >= today && a.DueDate <= weekAhead)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            var todayEventsCount = await _context.Events.AsNoTracking()
                .CountAsync(e => e.UserId == uid && e.StartTime >= today && e.StartTime < tomorrow);

            var focusMinutesToday = await _context.PomodoroSessions.AsNoTracking()
                .Where(p => p.UserId == uid && p.IsCompleted && p.CompletedAt >= today && p.CompletedAt < tomorrow)
                .SumAsync(p => (int?)p.DurationMinutes) ?? 0;

            var activeGoals = await _context.Goals.AsNoTracking()
                .CountAsync(g => g.UserId == uid && g.Status != "completed" && g.Status != "cancelled");

            var activeHabits = await _context.Habits.AsNoTracking()
                .CountAsync(h => h.UserId == uid && h.Active);

            var monthStartDate = DateOnly.FromDateTime(monthStart);
            var monthTx = await _context.Transactions.AsNoTracking()
                .Where(t => t.UserId == uid && t.TransactionDate >= monthStartDate)
                .Select(t => new { t.TransactionType, t.Amount })
                .ToListAsync();

            var monthIncome = monthTx.Where(t => t.TransactionType == "income").Sum(t => t.Amount);
            var monthExpense = monthTx.Where(t => t.TransactionType == "expense").Sum(t => t.Amount);

            var highTasks = openTasks.Count(t => t.Priority == "high" || t.Priority == "urgent");
            var dueSoonTasks = openTasks.Count(t => t.Deadline.HasValue && t.Deadline.Value.Date <= weekAhead);

            var insights = new List<AIInsightItem>();

            if (highTasks > 0)
            {
                insights.Add(new AIInsightItem
                {
                    Badge = "ƯU TIÊN",
                    Title = $"Có {highTasks} công việc mức ưu tiên cao chưa xong",
                    Detail = "Nên xử lý ít nhất 1 việc ưu tiên cao trong khung giờ tập trung đầu ngày.",
                    ActionUrl = Url.Action("Task", "Task") ?? "/Task/Task",
                    ActionLabel = "Mở danh sách việc"
                });
            }

            if (upcomingAssignments.Count > 0)
            {
                var next = upcomingAssignments[0];
                insights.Add(new AIInsightItem
                {
                    Badge = "HỌC TẬP",
                    Title = $"{upcomingAssignments.Count} bài tập đến hạn trong 7 ngày tới",
                    Detail = $"Gần nhất: {next.Title} (hạn {next.DueDate:dd/MM HH:mm}).",
                    ActionUrl = Url.Action("Assignments", "Study") ?? "/Study/Assignments",
                    ActionLabel = "Xem bài tập"
                });
            }

            if (focusMinutesToday == 0)
            {
                insights.Add(new AIInsightItem
                {
                    Badge = "TẬP TRUNG",
                    Title = "Chưa ghi nhận phiên Pomodoro nào hôm nay",
                    Detail = "Thử bắt đầu 1 phiên 25 phút để tạo đà hoàn thành công việc đầu tiên.",
                    ActionUrl = Url.Action("Pomodoro", "Time") ?? "/Time/Pomodoro",
                    ActionLabel = "Mở Pomodoro"
                });
            }
            else
            {
                insights.Add(new AIInsightItem
                {
                    Badge = "TẬP TRUNG",
                    Title = $"Đã tập trung {focusMinutesToday} phút hôm nay",
                    Detail = "Giữ nhịp nghỉ ngắn 5 phút giữa các phiên để duy trì hiệu suất.",
                    ActionUrl = Url.Action("Pomodoro", "Time") ?? "/Time/Pomodoro",
                    ActionLabel = "Tiếp tục Pomodoro"
                });
            }

            if (monthExpense > 0)
            {
                insights.Add(new AIInsightItem
                {
                    Badge = "TÀI CHÍNH",
                    Title = $"Chi tiêu tháng này: {monthExpense:N0} đ",
                    Detail = monthIncome > 0
                        ? $"Thu nhập ghi nhận: {monthIncome:N0} đ. Tỷ lệ chi/thu: {Math.Round(monthExpense * 100m / monthIncome, 0)}%."
                        : "Theo dõi các khoản chi nhỏ để giữ cân đối ngân sách tháng.",
                    ActionUrl = Url.Action("Index", "Finance") ?? "/Finance",
                    ActionLabel = "Xem tài chính"
                });
            }

            var vm = new AIAssistantViewModel
            {
                UserName = string.IsNullOrWhiteSpace(User.Identity?.Name) ? "Bạn" : User.Identity!.Name!,
                PendingTasks = openTasks.Count,
                HighPriorityTasks = highTasks,
                UpcomingDeadlines = dueSoonTasks + upcomingAssignments.Count,
                TodayEvents = todayEventsCount,
                FocusMinutesToday = focusMinutesToday,
                ActiveGoals = activeGoals,
                ActiveHabits = activeHabits,
                MonthIncome = monthIncome,
                MonthExpense = monthExpense,
                Insights = insights,
                TopPriorities = openTasks.Take(5).Select(t => new AIPrioritySuggestion
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Priority = t.Priority,
                    DeadlineLabel = t.Deadline.HasValue ? t.Deadline.Value.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture) : "Chưa đặt hạn"
                }).ToList()
            };

            return View(vm);
        }

        // POST /AI/Ask - Trả lời câu hỏi dựa trên dữ liệu thực tế của người dùng
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromBody] AIAskInput input)
        {
            var rawPrompt = (input?.Prompt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawPrompt))
                return BadRequest(new { ok = false, reply = "Vui lòng nhập câu hỏi hoặc yêu cầu." });

            var uid = User.GetUserId();
            var q = rawPrompt.ToLowerInvariant();
            var now = DateTime.Now;
            var today = now.Date;
            var weekAhead = today.AddDays(7);

            // Tạo nhanh công việc nếu người dùng yêu cầu: "tạo việc: ..." hoặc "thêm công việc ..."
            if (q.StartsWith("tạo việc:") || q.StartsWith("tạo công việc:") || q.StartsWith("thêm việc:"))
            {
                var idx = rawPrompt.IndexOf(':');
                var title = idx >= 0 ? rawPrompt[(idx + 1)..].Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(title))
                {
                    return Json(new { ok = true, reply = "Hãy nhập theo mẫu `Tạo việc: Tên công việc` để tôi thêm ngay vào danh sách của bạn." });
                }

                var task = new TaskEntity
                {
                    UserId = uid,
                    Title = title.Length > 200 ? title[..200] : title,
                    Priority = "medium",
                    Status = "todo",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    ok = true,
                    createdTask = new { id = task.Id, title = task.Title },
                    reply = $"Đã tạo công việc mới: **\"{task.Title}\"** (trạng thái Cần làm, ưu tiên Trung bình). Bạn có thể xem hoặc đặt hạn trong mục Công việc."
                });
            }

            var sb = new StringBuilder();

            if (q.Contains("hôm nay") || q.Contains("kế hoạch") || q.Contains("làm gì") || q.Contains("ưu tiên"))
            {
                var tasks = await _context.Tasks.AsNoTracking()
                    .Where(t => t.UserId == uid && t.Status != "done" && t.Status != "completed")
                    .OrderByDescending(t => t.Priority == "urgent" || t.Priority == "high")
                    .ThenBy(t => t.Deadline == null).ThenBy(t => t.Deadline)
                    .Take(4).ToListAsync();

                var events = await _context.Events.AsNoTracking()
                    .Where(e => e.UserId == uid && e.StartTime >= today && e.StartTime < today.AddDays(1))
                    .OrderBy(e => e.StartTime).Take(4).ToListAsync();

                sb.AppendLine("Dưới đây là gợi ý trọng tâm hôm nay dựa trên dữ liệu của bạn:");
                if (tasks.Any())
                {
                    sb.AppendLine("\n**Việc nên ưu tiên trước:**");
                    foreach (var t in tasks)
                    {
                        var dl = t.Deadline.HasValue ? $" (hạn {t.Deadline.Value:dd/MM HH:mm})" : "";
                        sb.AppendLine($"• {t.Title}{dl}");
                    }
                }
                else
                {
                    sb.AppendLine("\n• Hiện bạn không có công việc tồn đọng nào.");
                }

                if (events.Any())
                {
                    sb.AppendLine("\n**Lịch trình trong ngày:**");
                    foreach (var e in events)
                        sb.AppendLine($"• {e.StartTime:HH:mm} – {e.Title}");
                }

                sb.AppendLine("\n**Gợi ý nhịp làm việc:** Chia 2 phiên Pomodoro (25 phút tập trung / 5 phút nghỉ) cho việc ưu tiên cao nhất trước khi chuyển sang việc khác.");
            }
            else if (q.Contains("học") || q.Contains("bài tập") || q.Contains("deadline") || q.Contains("môn"))
            {
                var courseIds = _context.Courses.AsNoTracking().Where(c => c.UserId == uid).Select(c => c.Id);
                var assignments = await _context.Assignments.AsNoTracking()
                    .Where(a => courseIds.Contains(a.CourseId) && !a.IsSubmitted && a.DueDate >= today)
                    .OrderBy(a => a.DueDate).Take(5).ToListAsync();

                if (assignments.Any())
                {
                    sb.AppendLine($"Bạn đang có **{assignments.Count} bài tập** sắp tới:");
                    foreach (var a in assignments)
                        sb.AppendLine($"• {a.Title} — hạn {a.DueDate:dd/MM/yyyy HH:mm}");
                    sb.AppendLine("\nHãy gắn bài tập gần nhất vào Pomodoro để tập trung hoàn thành sớm.");
                }
                else
                {
                    sb.AppendLine("Hiện không có bài tập nào sắp đến hạn. Bạn có thể xem lại điểm số hoặc tài liệu ôn tập trong phân hệ Học tập.");
                }
            }
            else if (q.Contains("tiền") || q.Contains("chi tiêu") || q.Contains("tài chính") || q.Contains("ngân sách"))
            {
                var monthStartDate = DateOnly.FromDateTime(new DateTime(today.Year, today.Month, 1));
                var txs = await _context.Transactions.AsNoTracking()
                    .Where(t => t.UserId == uid && t.TransactionDate >= monthStartDate)
                    .Select(t => new { t.TransactionType, t.Amount }).ToListAsync();

                var inc = txs.Where(t => t.TransactionType == "income").Sum(t => t.Amount);
                var exp = txs.Where(t => t.TransactionType == "expense").Sum(t => t.Amount);

                sb.AppendLine($"**Tổng hợp tài chính tháng {today:MM/yyyy}:**");
                sb.AppendLine($"• Tổng thu: {inc:N0} đ");
                sb.AppendLine($"• Tổng chi: {exp:N0} đ");
                sb.AppendLine($"• Chênh lệch: {(inc - exp):N0} đ");
                if (inc > 0 && exp > inc * 0.8m)
                    sb.AppendLine("\nLưu ý: Mức chi hiện đã vượt 80% thu nhập tháng này, nên rà soát lại các khoản chi linh hoạt.");
            }
            else if (q.Contains("tập trung") || q.Contains("pomodoro") || q.Contains("thói quen") || q.Contains("mục tiêu"))
            {
                var habits = await _context.Habits.AsNoTracking()
                    .Where(h => h.UserId == uid && h.Active).Take(5).ToListAsync();
                var goals = await _context.Goals.AsNoTracking()
                    .Where(g => g.UserId == uid && g.Status != "completed").Take(4).ToListAsync();

                sb.AppendLine($"Bạn đang theo dõi **{goals.Count} mục tiêu** và **{habits.Count} thói quen** hoạt động.");
                if (goals.Any())
                {
                    sb.AppendLine("\n**Mục tiêu đang chạy:**");
                    foreach (var g in goals)
                        sb.AppendLine($"• {g.Title}");
                }
                sb.AppendLine("\nMẹo: Kết hợp 1 phiên Pomodoro 25 phút cho công việc gắn với mục tiêu chính mỗi ngày để giữ đà tiến độ.");
            }
            else
            {
                var openCount = await _context.Tasks.AsNoTracking()
                    .CountAsync(t => t.UserId == uid && t.Status != "done" && t.Status != "completed");
                var courseIds = _context.Courses.AsNoTracking().Where(c => c.UserId == uid).Select(c => c.Id);
                var assignCount = await _context.Assignments.AsNoTracking()
                    .CountAsync(a => courseIds.Contains(a.CourseId) && !a.IsSubmitted && a.DueDate >= today && a.DueDate <= weekAhead);

                sb.AppendLine($"Hiện tại bạn có **{openCount} công việc chưa hoàn thành** và **{assignCount} bài tập** đến hạn trong 7 ngày tới.");
                sb.AppendLine("\nBạn có thể hỏi tôi về:");
                sb.AppendLine("• *\"Hôm nay tôi nên làm gì?\"*");
                sb.AppendLine("• *\"Các bài tập sắp đến hạn\"*");
                sb.AppendLine("• *\"Tình hình chi tiêu tháng này\"*");
                sb.AppendLine("• *\"Tạo việc: Ôn tập chương 5\"*");
            }

            return Json(new { ok = true, reply = sb.ToString().Trim() });
        }

        // POST /AI/QuickCreateTask - Tạo nhanh 1 công việc từ gợi ý Trợ lý AI
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreateTask([FromForm] string title, [FromForm] string? priority)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["AIError"] = "Tên công việc không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var p = priority is "high" or "medium" or "low" ? priority : "medium";
            var now = DateTime.Now;
            _context.Tasks.Add(new TaskEntity
            {
                UserId = User.GetUserId(),
                Title = title.Trim().Length > 200 ? title.Trim()[..200] : title.Trim(),
                Priority = p,
                Status = "todo",
                CreatedAt = now,
                UpdatedAt = now
            });
            await _context.SaveChangesAsync();

            TempData["AISuccess"] = $"Đã thêm công việc \"{title.Trim()}\" vào danh sách.";
            return RedirectToAction(nameof(Index));
        }
    }
}
