using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.LifestyleManagement;
using LifeHub.Models.ViewModels.LifestyleManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Phân hệ Sinh hoạt cá nhân: 4 trang (Tổng quan, Giấc ngủ, Hoạt động thể chất, Kế hoạch).
    /// Chỉ có action GET nạp ViewModel (chưa có POST, không sửa model entity). Form trong View chạy mô phỏng (data-simulate).
    ///
    /// NGUỒN DỮ LIỆU
    ///   Giấc ngủ, vận động theo ngày: bảng lifestyle_logs (mỗi ngày một dòng, unique UserId + LogDate), dữ liệu thật.
    ///   Kế hoạch sinh hoạt: chưa có bảng nên dùng LifestylePlanSample (dữ liệu mẫu). Khi có bảng, chỉ sửa PlanData().
    ///
    /// KHI LÀM POST THẬT (Backend)
    ///   - Giấc ngủ: ghi lifestyle_logs (BedTime, WakeTime, SleepHours, SleepQuality, Notes); cùng ngày thì cập nhật dòng cũ.
    ///   - Hoạt động: hiện chỉ lưu được tổng phút + loại trong ngày; tên riêng, giờ bắt đầu, địa điểm, liên kết kế hoạch cần bảng/cột mới.
    ///   - Áp dụng kế hoạch vào lịch: tạo events với SourceType = "Lifestyle", SourceId = id hoạt động trong kế hoạch,
    ///     kiểm tra trùng (SourceType + SourceId + StartTime) trước khi thêm để áp dụng lại không tạo bản ghi trùng.
    /// </summary>
    [Authorize]
    public class LifestyleController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LifestyleController(ApplicationDbContext context) => _context = context;

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        private void Page(string title, string activePage)
        {
            ViewData["Title"] = title;
            // Phải trùng khóa mục con trong _Layout.cshtml để mục đó được tô sáng.
            ViewData["ActivePage"] = activePage;
        }

        // ------------------------------------------------------------------ GET

        // GET /Lifestyle?date=2026-11-24
        public async Task<IActionResult> Index(string? date)
        {
            Page("Sinh hoạt cá nhân", "LifestyleOverview");
            var userId = User.GetUserId();
            var today = Today;
            var day = DateOnly.TryParse(date, out var parsed) && parsed <= today.AddDays(14) ? parsed : today;

            var logs = await LoadLogs(userId, day.AddDays(-35), day);
            var sleep = SleepStats(logs, day, await LastSleepFallback(userId, logs, day));
            var activity = ActivityStats(logs, day);

            // Hoạt động trong kế hoạch của ngày đang xem (chỉ kế hoạch đang áp dụng).
            var plans = PlanData(day);
            var todayPlans = plans.Where(p => p.IsActive)
                .Select(p => new LifePlan
                {
                    Id = p.Id, Name = p.Name, Description = p.Description, StartDate = p.StartDate, EndDate = p.EndDate,
                    FrequencyLabel = p.FrequencyLabel, IsActive = p.IsActive,
                    Activities = p.Activities.Where(a => a.Days.Contains(day.DayOfWeek)).OrderBy(a => a.Time).ToList()
                })
                .Where(p => p.Activities.Count > 0).ToList();
            var scheduled = todayPlans.SelectMany(p => p.Activities).ToList();

            return View(new LifeOverviewViewModel
            {
                Date = day,
                IsToday = day == today,
                PrevKey = day.AddDays(-1).ToString("yyyy-MM-dd"),
                NextKey = day < today.AddDays(14) ? day.AddDays(1).ToString("yyyy-MM-dd") : null,
                LastSleepMinutes = sleep.LastMinutes,
                LastSleepNote = sleep.LastNote,
                ActivityMinutes = activity.TodayMinutes,
                ActivityNote = activity.TodayMinutes > 0 ? "Đã ghi nhận" : "Chưa ghi nhận",
                PlanDone = scheduled.Count(a => a.State == "done"),
                PlanScheduled = scheduled.Count,
                TodayPlans = todayPlans,
                Sleep = sleep,
                Activity = activity,
                Recent = logs.Where(l => (l.PhysicalActivityMinutes ?? 0) > 0).OrderByDescending(l => l.LogDate).Take(5).Select(ActivityItem).ToList(),
                PlanOptions = PlanOptions(plans)
            });
        }

        // GET /Lifestyle/Sleep?range=7|30|90
        public async Task<IActionResult> Sleep(string? range)
        {
            Page("Giấc ngủ", "LifestyleSleep");
            var userId = User.GetUserId();
            var today = Today;
            var r = range is "7" or "90" ? range : "30";
            var days = int.Parse(r);

            var logs = await LoadLogs(userId, today.AddDays(-Math.Max(35, days)), today);
            var from = today.AddDays(-(days - 1));
            var items = logs.Where(l => l.LogDate >= from && SleepMinutes(l).HasValue)
                .OrderByDescending(l => l.LogDate).Select(SleepItem).ToList();

            return View(new LifeSleepPageViewModel
            {
                Range = r,
                Stats = SleepStats(logs, today, await LastSleepFallback(userId, logs, today)),
                TotalInRange = items.Count,
                Items = items
            });
        }

        // GET /Lifestyle/Activity?range=7|30|90&category=walk|run|sport|workout|light|other
        public async Task<IActionResult> Activity(string? range, string? category)
        {
            Page("Hoạt động thể chất", "LifestyleActivity");
            var userId = User.GetUserId();
            var today = Today;
            var r = range is "7" or "90" ? range : "30";
            var days = int.Parse(r);
            var cat = LifeCategory.All.Any(c => c.Key == category) ? category! : "all";

            var logs = await LoadLogs(userId, today.AddDays(-Math.Max(35, days)), today);
            var from = today.AddDays(-(days - 1));
            var items = logs.Where(l => l.LogDate >= from && (l.PhysicalActivityMinutes ?? 0) > 0)
                .OrderByDescending(l => l.LogDate).Select(ActivityItem)
                .Where(a => cat == "all" || a.CategoryKey == cat).ToList();

            return View(new LifeActivityPageViewModel
            {
                Range = r,
                Category = cat,
                Stats = ActivityStats(logs, today),
                TotalInRange = items.Count,
                Items = items,
                PlanOptions = PlanOptions(PlanData(today))
            });
        }

        // GET /Lifestyle/Plan?id=1
        public IActionResult Plan(int? id)
        {
            Page("Kế hoạch sinh hoạt", "LifestylePlan");
            var plans = PlanData(Today);
            var selected = plans.FirstOrDefault(p => p.Id == id) ?? plans.FirstOrDefault(p => p.IsActive) ?? plans.FirstOrDefault();
            return View(new LifePlanPageViewModel
            {
                TotalCount = plans.Count,
                ActiveCount = plans.Count(p => p.IsActive),
                PausedCount = plans.Count(p => !p.IsActive),
                ActivityCount = plans.Sum(p => p.Activities.Count),
                Plans = plans,
                Selected = selected
            });
        }

        // ------------------------------------------------------------------ NGUỒN DỮ LIỆU

        private Task<List<LifestyleLog>> LoadLogs(int userId, DateOnly from, DateOnly to) =>
            _context.LifestyleLogs.AsNoTracking()
                .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to)
                .OrderBy(l => l.LogDate).ToListAsync();

        /// <summary>Giấc ngủ gần nhất có thể nằm ngoài khoảng đã nạp; chỉ truy vấn thêm khi khoảng nạp không có.</summary>
        private async Task<LifestyleLog?> LastSleepFallback(int userId, List<LifestyleLog> logs, DateOnly day)
        {
            if (logs.Any(l => l.LogDate <= day && SleepMinutes(l).HasValue)) return null;
            var older = await _context.LifestyleLogs.AsNoTracking()
                .Where(l => l.UserId == userId && l.LogDate <= day && (l.SleepHours != null || (l.BedTime != null && l.WakeTime != null)))
                .OrderByDescending(l => l.LogDate).FirstOrDefaultAsync();
            return older;
        }

        /// <summary>Kế hoạch sinh hoạt: DỮ LIỆU MẪU. Thay dòng này bằng truy vấn DB khi có bảng.</summary>
        private static List<LifePlan> PlanData(DateOnly day) => LifestylePlanSample.Build(day, DateTime.Now);

        private static List<LifePlanOption> PlanOptions(List<LifePlan> plans) =>
            plans.Where(p => p.IsActive).SelectMany(p => p.Activities)
                .OrderBy(a => a.PlanId).ThenBy(a => a.Time)
                .Select(a => new LifePlanOption { Id = a.Id, Text = $"{a.PlanName} · {a.Name} {a.TimeLabel}" }).ToList();

        // ------------------------------------------------------------------ TÍNH TOÁN

        private static int? SleepMinutes(LifestyleLog l)
        {
            if (l.SleepHours.HasValue) return (int)Math.Round(l.SleepHours.Value * 60);
            if (l.BedTime.HasValue && l.WakeTime.HasValue)
            {
                var m = (l.WakeTime.Value.ToTimeSpan() - l.BedTime.Value.ToTimeSpan()).TotalMinutes;
                if (m <= 0) m += 1440;      // đi ngủ trước nửa đêm, thức dậy hôm sau
                return (int)m;
            }
            return null;
        }

        private static LifeSleepItem SleepItem(LifestyleLog l) => new()
        {
            Id = l.Id, Date = l.LogDate, Bed = l.BedTime, Wake = l.WakeTime,
            Minutes = SleepMinutes(l), Quality = l.SleepQuality, Notes = l.Notes ?? ""
        };

        private static LifeActivityItem ActivityItem(LifestyleLog l) => new()
        {
            Id = l.Id, Date = l.LogDate,
            Name = string.IsNullOrWhiteSpace(l.ActivityType) ? "Hoạt động thể chất" : l.ActivityType!.Trim(),
            CategoryKey = LifeCategory.FromText(l.ActivityType),
            Minutes = l.PhysicalActivityMinutes ?? 0, Notes = l.Notes ?? ""
        };

        /// <summary>Trung bình giờ trong ngày. Giờ trước <paramref name="wrapBefore"/> (phút) được coi là sang ngày hôm sau (để 23:30 và 00:30 cho kết quả đúng).</summary>
        private static string AvgClock(IEnumerable<TimeOnly> times, int wrapBefore)
        {
            var mins = times.Select(t => { var m = t.Hour * 60 + t.Minute; return m < wrapBefore ? m + 1440 : m; }).ToList();
            return mins.Count == 0 ? "—" : LifeFmt.Clock((int)Math.Round(mins.Average()));
        }

        private static LifeSleepStats SleepStats(List<LifestyleLog> logs, DateOnly day, LifestyleLog? fallbackLast)
        {
            var withSleep = logs.Where(l => l.LogDate <= day && SleepMinutes(l).HasValue).OrderByDescending(l => l.LogDate).ToList();
            var last = withSleep.FirstOrDefault() ?? fallbackLast;

            var window = Enumerable.Range(0, 7).Select(i => day.AddDays(i - 6)).ToList();
            var byDay = withSleep.ToDictionary(l => l.LogDate);
            var stats = new LifeSleepStats();

            if (last != null)
            {
                stats.LastMinutes = SleepMinutes(last);
                stats.LastNote = last.LogDate == day.AddDays(-1) ? "Đêm qua" : $"Đêm {last.LogDate:dd/MM}";
            }

            foreach (var d in window)
            {
                byDay.TryGetValue(d, out var l);
                var m = l == null ? null : SleepMinutes(l);
                stats.Bars.Add(new LifeSleepBar
                {
                    Label = d.ToString("dd/MM"), Minutes = m, IsToday = d == day,
                    Percent = m.HasValue ? Math.Min(100, (int)Math.Round(m.Value * 100d / 600)) : 0     // thang 0 đến 10 giờ
                });

                // Thanh giờ ngủ: trục từ 20:00 đến 12:00 hôm sau (960 phút).
                var span = new LifeSleepSpan { Label = d.ToString("dd/MM") };
                if (l != null && l.BedTime.HasValue && m.HasValue)
                {
                    var bedMin = l.BedTime.Value.Hour * 60 + l.BedTime.Value.Minute;
                    var left = ((bedMin - 1200) % 1440 + 1440) % 1440;
                    var width = Math.Min(m.Value, 960 - Math.Min(left, 960));
                    span.HasData = true;
                    span.LeftPercent = Math.Round(Math.Min(left, 960) * 100d / 960, 1);
                    span.WidthPercent = Math.Round(Math.Max(width, 10) * 100d / 960, 1);
                    span.BedLabel = LifeFmt.Clock(l.BedTime);
                    span.WakeLabel = l.WakeTime.HasValue ? LifeFmt.Clock(l.WakeTime) : LifeFmt.Clock(bedMin + m.Value);
                }
                stats.Spans.Add(span);
            }

            var recorded = window.Where(byDay.ContainsKey).Select(d => byDay[d]).ToList();
            stats.RecordedDays = recorded.Count;
            stats.AverageMinutes = recorded.Count == 0 ? null : (int)Math.Round(recorded.Average(l => SleepMinutes(l)!.Value));
            stats.AverageBed = AvgClock(recorded.Where(l => l.BedTime.HasValue).Select(l => l.BedTime!.Value), 720);
            stats.AverageWake = AvgClock(recorded.Where(l => l.WakeTime.HasValue).Select(l => l.WakeTime!.Value), 0);
            return stats;
        }

        private static LifeActivityStats ActivityStats(List<LifestyleLog> logs, DateOnly day)
        {
            int Min(LifestyleLog l) => l.PhysicalActivityMinutes ?? 0;
            var weekStart = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));            // thứ Hai của tuần chứa ngày đang xem
            var elapsed = day.DayNumber - weekStart.DayNumber;                         // số ngày đã qua trong tuần
            var lastWeekStart = weekStart.AddDays(-7);

            var week = logs.Where(l => l.LogDate >= weekStart && l.LogDate <= day && Min(l) > 0).ToList();
            var lastWeek = logs.Where(l => l.LogDate >= lastWeekStart && l.LogDate <= lastWeekStart.AddDays(elapsed)).Sum(l => Min(l));
            var thisWeek = week.Sum(l => Min(l));

            var stats = new LifeActivityStats
            {
                TodayMinutes = logs.Where(l => l.LogDate == day).Sum(l => Min(l)),
                WeekMinutes = thisWeek,
                WeekSessions = week.Count
            };

            var latest = logs.Where(l => l.LogDate <= day && Min(l) > 0).OrderByDescending(l => l.LogDate).FirstOrDefault();
            if (latest != null)
            {
                var item = ActivityItem(latest);
                stats.LatestName = item.Name;
                stats.LatestNote = $"{item.DurationLabel} · {(latest.LogDate == day ? "hôm nay" : latest.LogDate.ToString("dd/MM"))}";
            }

            // Cột 7 ngày: thang tối thiểu 60 phút để cột không phóng đại khi số liệu nhỏ.
            var days = Enumerable.Range(0, 7).Select(i => day.AddDays(i - 6)).ToList();
            var mins = days.Select(d => logs.Where(l => l.LogDate == d).Sum(l => Min(l))).ToList();
            var scale = Math.Max(60, mins.Max());
            for (int i = 0; i < 7; i++)
                stats.Bars.Add(new LifeDayBar { Label = days[i].ToString("dd/MM"), Minutes = mins[i], IsToday = days[i] == day, Percent = (int)Math.Round(mins[i] * 100d / scale) });

            // Phân loại 30 ngày gần nhất.
            var recent = logs.Where(l => l.LogDate > day.AddDays(-30) && l.LogDate <= day && Min(l) > 0).Select(ActivityItem).ToList();
            var bands = LifeCategory.All.Select(c => new LifeBand
            {
                Key = c.Key, Label = c.Label,
                Minutes = recent.Where(a => a.CategoryKey == c.Key).Sum(a => a.Minutes),
                Sessions = recent.Count(a => a.CategoryKey == c.Key)
            }).Where(b => b.Minutes > 0).OrderByDescending(b => b.Minutes).ToList();
            var top = bands.Count == 0 ? 1 : bands.Max(b => b.Minutes);
            foreach (var b in bands) b.Percent = (int)Math.Round(b.Minutes * 100d / top);
            stats.Bands = bands;

            // So sánh với cùng kỳ tuần trước (cùng số ngày đã qua).
            var peak = Math.Max(1, Math.Max(thisWeek, lastWeek));
            var diff = thisWeek - lastWeek;
            stats.Compare = new LifeWeekCompare
            {
                ThisWeekMinutes = thisWeek, LastWeekMinutes = lastWeek,
                ThisPercent = (int)Math.Round(thisWeek * 100d / peak), LastPercent = (int)Math.Round(lastWeek * 100d / peak),
                DeltaTone = diff > 0 ? "up" : diff < 0 ? "down" : "flat",
                DeltaLabel = thisWeek == 0 && lastWeek == 0 ? "Chưa có dữ liệu để so sánh"
                    : lastWeek == 0 ? "Tuần trước chưa có ghi nhận"
                    : diff > 0 ? $"Tăng {LifeFmt.Duration(diff)} so với cùng kỳ tuần trước"
                    : diff < 0 ? $"Giảm {LifeFmt.Duration(-diff)} so với cùng kỳ tuần trước"
                    : "Bằng cùng kỳ tuần trước"
            };
            return stats;
        }
    }
}
