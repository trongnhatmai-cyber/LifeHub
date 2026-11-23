using System.Text.RegularExpressions;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.HabitManagement;
using LifeHub.Models.ViewModels.HabitManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Phân hệ Thói quen: chỉ có action GET nạp ViewModel (chưa có POST, không sửa model entity).
    /// Form trong View đang chạy mô phỏng (data-simulate). Khi có action thật, bỏ data-simulate.
    /// Check-in thật cần: kiểm tra trùng (HabitId + CheckDate), ghi HabitLog, cập nhật streak.
    /// Cột frequency / color_code là varchar: lưu mã ASCII (daily, weekly, monthly, #RRGGBB).
    /// </summary>
    [Authorize]
    public class HabitController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HabitController(ApplicationDbContext context) => _context = context;

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        private void Page(string title, string activePage)
        {
            ViewData["Title"] = title;
            // Phải trùng khóa mục con trong _Layout.cshtml để mục đó được tô sáng.
            ViewData["ActivePage"] = activePage;
        }

        // ------------------------------------------------------------------ GET

        // GET /Habit
        public async Task<IActionResult> Index()
        {
            Page("Tổng quan thói quen", "HabitOverview");
            var today = Today;
            var d = await Load(30, includePaused: false);
            var cards = d.Habits.Select(h => Card(h, d, today)).ToList();

            return View(new HabitOverviewViewModel
            {
                DateLabel = today.ToString("dd/MM/yyyy"),
                ActiveCount = cards.Count,
                DoneToday = cards.Count(c => c.State == "done"),
                RateToday = cards.Count == 0 ? 0 : (int)Math.Round(cards.Count(c => c.State == "done") * 100d / cards.Count),
                BestStreak = cards.Count == 0 ? 0 : cards.Max(c => c.CurrentStreak),
                Today = cards.OrderBy(c => c.State == "done").ThenBy(c => c.ReminderTime == "" ? "99:99" : c.ReminderTime).ToList(),
                TopStreaks = cards.Where(c => c.CurrentStreak > 0).OrderByDescending(c => c.CurrentStreak).Take(4).ToList(),
                Attention = cards.Where(c => c.State != "done" && c.CurrentStreak > 0).OrderByDescending(c => c.CurrentStreak).Take(4).ToList(),
                Heatmap = Heatmap(d, 30)
            });
        }

        // GET /Habit/HabitList?status=active|paused&q=...
        public async Task<IActionResult> HabitList(string? status, string? frequency, string? sort, string? q)
        {
            Page("Danh sách thói quen", "HabitList");
            var today = Today;
            var d = await Load(30, includePaused: true);
            var all = d.Habits.Select(h => Card(h, d, today)).ToList();
            var st = (status ?? "all").ToLowerInvariant();
            IEnumerable<HabitCardItem> query = all;
            if (st == "active") query = query.Where(c => c.Active);
            else if (st == "paused") query = query.Where(c => !c.Active);
            else st = "all";
            var fr = (frequency ?? "all").ToLowerInvariant();
            if (fr is "daily" or "weekly" or "monthly") query = query.Where(c => c.Frequency == fr); else fr = "all";
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(c => c.Title.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase));
            var so = (sort ?? "name").ToLowerInvariant();
            query = so switch
            {
                "streak" => query.OrderByDescending(c => c.CurrentStreak).ThenBy(c => c.Title),
                "newest" => query.OrderByDescending(c => c.Id),
                _ => query.OrderBy(c => c.Title)
            };

            return View(new HabitListViewModel
            {
                Status = st, Frequency = fr, Sort = so, Q = q,
                TotalCount = all.Count,
                ActiveCount = all.Count(c => c.Active),
                PausedCount = all.Count(c => !c.Active),
                Items = query.ToList(),
                Goals = d.GoalOptions
            });
        }

        // GET /Habit/HabitTracking?date=2026-11-22
        public async Task<IActionResult> HabitTracking(string? date)
        {
            Page("Theo dõi thói quen", "HabitTracking");
            var today = Today;
            var day = DateOnly.TryParse(date, out var parsed) && parsed <= today ? parsed : today;
            var d = await Load(today.DayNumber - day.DayNumber + 30, includePaused: false);
            var cards = d.Habits.Where(h => h.CreatedAt.Date <= day.ToDateTime(TimeOnly.MinValue)).Select(h => Card(h, d, day)).ToList();

            return View(new HabitTrackingViewModel
            {
                Date = day,
                IsToday = day == today,
                PrevKey = day.AddDays(-1).ToString("yyyy-MM-dd"),
                NextKey = day < today ? day.AddDays(1).ToString("yyyy-MM-dd") : null,
                DoneCount = cards.Count(c => c.State == "done"),
                Total = cards.Count,
                Items = cards.OrderBy(c => c.State == "done").ThenBy(c => c.Title).ToList(),
                Goals = d.GoalOptions
            });
        }

        // GET /Habit/HabitStreak
        public async Task<IActionResult> HabitStreak()
        {
            Page("Chuỗi duy trì", "HabitStatistics");   // màn phụ của Thống kê: sidebar vẫn tô sáng Thống kê
            var today = Today;
            var d = await Load(30, includePaused: false);
            var items = d.Habits.Select(h => new HabitStreakItem
            {
                Habit = Card(h, d, today),
                Days = Enumerable.Range(0, 30).Select(i =>
                {
                    var date = today.AddDays(-29 + i);
                    var done = d.Logs.Any(l => l.HabitId == h.Id && l.CheckDate == date && l.IsCompleted);
                    return new HabitDayCell { Date = date, Done = done ? 1 : 0, Total = 1, Level = done ? 4 : 0, IsToday = date == today };
                }).ToList()
            }).OrderByDescending(x => x.Habit.CurrentStreak).ThenBy(x => x.Habit.Title).ToList();

            return View(new HabitStreakViewModel
            {
                BestCurrent = items.Count == 0 ? 0 : items.Max(x => x.Habit.CurrentStreak),
                BestEver = items.Count == 0 ? 0 : items.Max(x => x.Habit.LongestStreak),
                Streaking = items.Count(x => x.Habit.CurrentStreak > 0),
                Broken = items.Count(x => x.Habit.CurrentStreak == 0),
                Items = items
            });
        }

        // GET /Habit/HabitStatistics?range=7|30|90
        public async Task<IActionResult> HabitStatistics(int? range)
        {
            Page("Thống kê thói quen", "HabitStatistics");
            var today = Today;
            var days = range is 7 or 90 ? range.Value : 30;
            var from = today.AddDays(-(days - 1));
            var d = await Load(days, includePaused: false);

            // Số ngày hiệu lực của mỗi thói quen trong khoảng (không tính trước ngày tạo).
            int Span(Habit h, DateOnly start, DateOnly end)
            {
                var created = DateOnly.FromDateTime(h.CreatedAt);
                var s = start > created ? start : created;
                return end < s ? 0 : end.DayNumber - s.DayNumber + 1;
            }
            double Done(int habitId, DateOnly start, DateOnly end) =>
                d.Logs.Where(l => l.HabitId == habitId && l.IsCompleted && l.CheckDate >= start && l.CheckDate <= end).Sum(l => Math.Max(1, l.CountValue));

            var rows = d.Habits.Select(h =>
            {
                var key = HabitFrequency.Normalize(h.Frequency);
                var expected = HabitFrequency.Expected(key, h.TargetPerPeriod, Span(h, from, today));
                var done = Done(h.Id, from, today);
                return new HabitStatRow
                {
                    HabitId = h.Id, CurrentStreak = h.CurrentStreak, LongestStreak = Math.Max(h.LongestStreak, h.CurrentStreak), Title = h.Title, Color = SafeColor(h.ColorCode), FrequencyLabel = HabitFrequency.Label(key),
                    Done = (int)done, Expected = Math.Round(expected, 1), Rate = HabitFrequency.Rate(done, expected)
                };
            }).Where(r => r.Expected > 0).ToList();

            // Xu hướng: tối đa 10 mốc (7 ngày = 7 mốc, 30 ngày = 10 mốc x 3 ngày, 90 ngày = 10 mốc x 9 ngày).
            var size = days == 7 ? 1 : days == 30 ? 3 : 9;
            var trend = Enumerable.Range(0, days / size).Select(b =>
            {
                var s = from.AddDays(b * size);
                var e = s.AddDays(size - 1);
                double done = 0, expected = 0;
                foreach (var h in d.Habits)
                {
                    expected += HabitFrequency.Expected(HabitFrequency.Normalize(h.Frequency), h.TargetPerPeriod, Span(h, s, e));
                    done += Done(h.Id, s, e);
                }
                return new HabitTrendPoint { Label = size == 1 ? s.ToString("dd/MM") : $"{s:dd/MM}", Rate = HabitFrequency.Rate(done, expected) };
            }).ToList();

            // Ngày bỏ lỡ: chỉ thói quen hằng ngày, không tính hôm nay (vẫn còn thời gian check-in).
            var missed = new List<HabitMissedItem>();
            foreach (var h in d.Habits.Where(x => HabitFrequency.Normalize(x.Frequency) == HabitFrequency.Daily))
            {
                var created = DateOnly.FromDateTime(h.CreatedAt);
                for (var date = from; date < today; date = date.AddDays(1))
                {
                    if (date < created) continue;
                    if (!d.Logs.Any(l => l.HabitId == h.Id && l.CheckDate == date && l.IsCompleted))
                        missed.Add(new HabitMissedItem { HabitTitle = h.Title, Color = SafeColor(h.ColorCode), Date = date });
                }
            }

            var totalExpected = rows.Sum(r => r.Expected);
            var ordered = rows.OrderByDescending(r => r.Rate).ThenBy(r => r.Title).ToList();
            return View(new HabitStatisticsViewModel
            {
                Range = days,
                HabitCount = d.Habits.Count,
                OverallRate = HabitFrequency.Rate(rows.Sum(r => r.Done), totalExpected),
                TotalCompletions = d.Logs.Where(l => l.IsCompleted).Sum(l => Math.Max(1, l.CountValue)),
                MissedDays = missed.Count,
                BestCurrent = d.Habits.Count == 0 ? 0 : d.Habits.Max(h => h.CurrentStreak),
                BestEver = d.Habits.Count == 0 ? 0 : d.Habits.Max(h => Math.Max(h.LongestStreak, h.CurrentStreak)),
                Trend = trend,
                Rows = ordered,
                Missed = missed.OrderByDescending(m => m.Date).Take(8).ToList(),
                Heatmap = Heatmap(d, days),
                Insights = Insights(ordered, d.Logs, days)
            });
        }

        private static readonly string[] DayNames = { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy" };

        /// <summary>Nhận xét: chỉ sinh từ dữ liệu thật, bằng quy tắc thống kê đơn giản (chưa dùng AI).</summary>
        private static List<string> Insights(List<HabitStatRow> rows, List<HabitLog> logs, int days)
        {
            var list = new List<string>();
            if (rows.Count == 0) return list;
            var best = rows.First();
            if (best.Done > 0) list.Add($"{best.Title} là thói quen bạn duy trì đều nhất trong {days} ngày qua ({best.Rate}%).");
            var low = rows.Last();
            if (rows.Count > 1 && low.Rate < 50 && low.HabitId != best.HabitId)
                list.Add($"{low.Title} đang cần được quan tâm: tỷ lệ hiện là {low.Rate}%. Bạn có thể thử giảm mục tiêu mỗi kỳ hoặc đổi giờ nhắc.");
            var done = logs.Where(l => l.IsCompleted).ToList();
            if (done.Count >= 5)
            {
                var top = done.GroupBy(l => (int)l.CheckDate.DayOfWeek).OrderByDescending(g => g.Count()).First();
                list.Add($"Bạn check-in nhiều nhất vào {DayNames[top.Key].ToLowerInvariant()} ({top.Count()} lần).");
            }
            return list;
        }

        // ------------------------------------------------------------------ Helpers

        private sealed class HabitData
        {
            public List<Habit> Habits { get; set; } = new();
            public List<HabitLog> Logs { get; set; } = new();
            public Dictionary<int, string> GoalTitles { get; set; } = new();
            public List<HabitGoalOption> GoalOptions { get; set; } = new();
        }

        /// <summary>Nạp thói quen của user và log trong `days` ngày gần nhất (kể cả hôm nay).</summary>
        private async Task<HabitData> Load(int days, bool includePaused)
        {
            var uid = User.GetUserId();
            var from = Today.AddDays(-(days - 1));
            var q = _context.Habits.AsNoTracking().Where(h => h.UserId == uid);
            if (!includePaused) q = q.Where(h => h.Active);
            var habits = await q.OrderBy(h => h.Title).ToListAsync();
            var ids = habits.Select(h => h.Id).ToArray();
            var logs = await _context.HabitLogs.AsNoTracking()
                .Where(l => ids.Contains(l.HabitId) && l.CheckDate >= from && l.CheckDate <= Today).ToListAsync();
            var goals = await _context.Goals.AsNoTracking().Where(g => g.UserId == uid).OrderBy(g => g.Title)
                .Select(g => new { g.Id, g.Title }).ToListAsync();
            return new HabitData
            {
                Habits = habits, Logs = logs,
                GoalTitles = goals.ToDictionary(g => g.Id, g => g.Title),
                GoalOptions = goals.Select(g => new HabitGoalOption { Id = g.Id, Title = g.Title }).ToList()
            };
        }

        private static HabitCardItem Card(Habit h, HabitData d, DateOnly day)
        {
            var key = HabitFrequency.Normalize(h.Frequency);
            var log = d.Logs.FirstOrDefault(l => l.HabitId == h.Id && l.CheckDate == day && l.IsCompleted);
            var today = Today;
            var from7 = today.AddDays(-6);
            var done7 = d.Logs.Where(l => l.HabitId == h.Id && l.IsCompleted && l.CheckDate >= from7).Sum(l => Math.Max(1, l.CountValue));
            var span7 = Math.Min(7, Math.Max(1, today.DayNumber - DateOnly.FromDateTime(h.CreatedAt).DayNumber + 1));
            var win = key == HabitFrequency.Weekly ? 7 : key == HabitFrequency.Monthly ? 30 : 1;
            var winFrom = day.AddDays(-(win - 1));
            var period = d.Logs.Where(l => l.HabitId == h.Id && l.IsCompleted && l.CheckDate >= winFrom && l.CheckDate <= day).Sum(l => Math.Max(1, l.CountValue));
            var target = Math.Max(1, h.TargetPerPeriod);
            var state = key == HabitFrequency.Daily
                ? (period >= target ? "done" : period > 0 ? "partial" : "todo")
                : (log != null ? "done" : period > 0 && period < target ? "partial" : "todo");
            return new HabitCardItem
            {
                PeriodDone = period, State = state,
                Id = h.Id, Title = h.Title, Color = SafeColor(h.ColorCode), Frequency = key, TargetPerPeriod = Math.Max(1, h.TargetPerPeriod),
                ReminderTime = h.ReminderTime?.ToString("HH:mm") ?? "",
                CurrentStreak = h.CurrentStreak, LongestStreak = Math.Max(h.LongestStreak, h.CurrentStreak), Active = h.Active,
                GoalId = h.GoalId, GoalTitle = h.GoalId.HasValue && d.GoalTitles.TryGetValue(h.GoalId.Value, out var t) ? t : "",
                DateKey = day.ToString("yyyy-MM-dd"), DoneOnDate = log != null, CountOnDate = log?.CountValue ?? 0, NoteOnDate = log?.Note ?? "",
                Rate7 = HabitFrequency.Rate(done7, HabitFrequency.Expected(key, h.TargetPerPeriod, span7))
            };
        }

        /// <summary>Heatmap: mỗi ô = số thói quen có check-in / số thói quen đang bật (đã tạo) ở ngày đó.</summary>
        private static List<HabitDayCell> Heatmap(HabitData d, int days)
        {
            var today = Today;
            return Enumerable.Range(0, days).Select(i =>
            {
                var date = today.AddDays(-(days - 1) + i);
                var live = d.Habits.Where(h => DateOnly.FromDateTime(h.CreatedAt) <= date).Select(h => h.Id).ToHashSet();
                var done = d.Logs.Where(l => l.CheckDate == date && l.IsCompleted && live.Contains(l.HabitId)).Select(l => l.HabitId).Distinct().Count();
                var r = live.Count == 0 ? 0d : (double)done / live.Count;
                return new HabitDayCell { Date = date, Done = done, Total = live.Count, IsToday = date == today,
                    Level = done == 0 ? 0 : r <= .25 ? 1 : r <= .5 ? 2 : r <= .75 ? 3 : 4 };
            }).ToList();
        }

        private static string SafeColor(string? c) => c != null && Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$") ? c : "#4f46e5";
    }
}
