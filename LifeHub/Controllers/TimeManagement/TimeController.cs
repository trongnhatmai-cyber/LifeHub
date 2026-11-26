using System.Globalization;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.TimeManagement;
using LifeHub.Models.ViewModels.TimeManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Quản lý thời gian: Tổng quan, Lịch cá nhân, Sự kiện, Pomodoro.
    /// Giai đoạn này chỉ ĐỌC dữ liệu và dựng ViewModel; các form tạo/sửa/xóa sự kiện là mô phỏng
    /// cho đến khi Back-end đổi Entity Event (giờ kết thúc, nhắc nhở, lặp lại, trạng thái hủy).
    /// </summary>
    [Authorize]
    public class TimeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TimeController(ApplicationDbContext context) => _context = context;

        // Nhóm nguồn: khóa, nhãn, màu nhận diện, nơi chỉnh sửa
        private static readonly Dictionary<string, (string Label, string Color, string Origin, string? Controller, string? Action)> Sources = new()
        {
            ["study"] = ("Học tập", "#2563eb", "Học tập", "Study", "Schedule"),
            ["task"] = ("Công việc", "#ea580c", "Công việc", "Task", "Index"),
            ["project"] = ("Dự án", "#16a34a", "Dự án", "Project", "Index"),
            ["life"] = ("Sinh hoạt", "#059669", "Sinh hoạt", "Lifestyle", "Index"),
            ["personal"] = ("Cá nhân", "#7c3aed", "Quản lý thời gian", null, null)
        };
        private static readonly string[] Dow = { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
        private static readonly string[] DayNames = { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy" };

        private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string DateLabel(DateTime d) => $"{DayNames[(int)d.DayOfWeek]}, {d:dd/MM/yyyy}";
        private static DateTime WeekStart(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
        private static DateTime ParseDate(string? s) =>
            DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.Date : DateTime.Today;

        private static string Minutes(int m)
        {
            if (m <= 0) return "0p";
            var h = m / 60; var r = m % 60;
            return h == 0 ? $"{r}p" : r == 0 ? $"{h}h" : $"{h}h {r:00}p";
        }

        private void Page(string title, string active) { ViewData["Title"] = title; ViewData["ActivePage"] = active; }

        // ================================================================== GET

        // GET /Time?date=yyyy-MM-dd
        public async Task<IActionResult> Index(string? date)
        {
            Page("Quản lý thời gian", "TimeOverview");
            var uid = User.GetUserId();
            var day = ParseDate(date);
            var ws = WeekStart(day);
            var week = await LoadEventsAsync(uid, ws, ws.AddDays(7));
            var dayEvents = week.Where(e => e.Start.Date == day).OrderBy(e => e.Start).ToList();

            var focus = await _context.PomodoroSessions.AsNoTracking()
                .Where(p => p.UserId == uid && p.IsCompleted && p.CompletedAt != null && p.CompletedAt >= day && p.CompletedAt < day.AddDays(1))
                .Select(p => p.DurationMinutes).ToListAsync();

            // Phân bổ thời gian theo nhóm trong tuần chứa ngày đang xem
            var groups = new[] { ("study", "Học tập"), ("task", "Công việc"), ("project", "Dự án"), ("life", "Sinh hoạt cá nhân"), ("personal", "Khác") };
            var totalMin = week.Sum(e => e.DurationMinutes);
            var slices = new List<TimeSlice>();
            double offset = 0;
            foreach (var (key, label) in groups)
            {
                var min = week.Where(e => e.Source == key).Sum(e => e.DurationMinutes);
                if (min <= 0) continue;
                var pct = totalMin == 0 ? 0 : min * 100.0 / totalMin;
                slices.Add(new TimeSlice { Label = label, Color = Sources[key].Color, Minutes = min, Percent = (int)Math.Round(pct), Offset = offset, MinutesText = Minutes(min), });
                offset += pct;
            }

            var weekDone = week.Count(e => e.Status == "done");
            return View(new TimeOverviewViewModel
            {
                DateIso = Iso(day),
                DateLabel = DateLabel(day),
                TodayIso = Iso(DateTime.Today),
                TodayEvents = dayEvents,
                PlannedText = Minutes(dayEvents.Sum(e => e.DurationMinutes)),
                DoneCount = dayEvents.Count(e => e.Status == "done"),
                TotalCount = dayEvents.Count,
                FocusText = Minutes(focus.Sum()),
                FocusSessions = focus.Count,
                Allocation = slices,
                AllocationTotalText = Minutes(totalMin),
                WeekLabel = $"{ws:dd/MM} – {ws.AddDays(6):dd/MM/yyyy}",
                WeekPlannedText = Minutes(totalMin),
                WeekDone = weekDone,
                WeekRemaining = week.Count - weekDone,
                WeekPercent = week.Count == 0 ? 0 : weekDone * 100 / week.Count
            });
        }

        // GET /Time/Calendar?view=day|week|month&date=yyyy-MM-dd
        public async Task<IActionResult> Calendar(string? view, string? date)
        {
            Page("Lịch cá nhân", "TimeCalendar");
            var uid = User.GetUserId();
            view = view is "day" or "month" ? view : "week";
            var focus = ParseDate(date);
            var today = DateTime.Today;
            var vm = new TimeCalendarViewModel { View = view, DateIso = Iso(focus), TodayIso = Iso(today) };

            DateTime rangeFrom, rangeTo;
            if (view == "day")
            {
                rangeFrom = focus; rangeTo = focus.AddDays(1);
                vm.RangeLabel = DateLabel(focus);
                vm.PrevIso = Iso(focus.AddDays(-1)); vm.NextIso = Iso(focus.AddDays(1));
            }
            else if (view == "week")
            {
                rangeFrom = WeekStart(focus); rangeTo = rangeFrom.AddDays(7);
                vm.RangeLabel = rangeFrom.Month == rangeTo.AddDays(-1).Month
                    ? $"{rangeFrom.Day} – {rangeTo.AddDays(-1).Day} tháng {rangeFrom.Month}, {rangeFrom.Year}"
                    : $"{rangeFrom.Day}/{rangeFrom.Month} – {rangeTo.AddDays(-1).Day}/{rangeTo.AddDays(-1).Month}, {rangeTo.AddDays(-1).Year}";
                vm.PrevIso = Iso(focus.AddDays(-7)); vm.NextIso = Iso(focus.AddDays(7));
            }
            else
            {
                var first = new DateTime(focus.Year, focus.Month, 1);
                rangeFrom = WeekStart(first); rangeTo = WeekStart(first.AddMonths(1).AddDays(-1)).AddDays(7);
                vm.RangeLabel = $"Tháng {focus.Month}, {focus.Year}";
                vm.PrevIso = Iso(first.AddMonths(-1)); vm.NextIso = Iso(first.AddMonths(1));
            }

            var events = await LoadEventsAsync(uid, rangeFrom, rangeTo);
            vm.EventCount = events.Count;
            for (var d = rangeFrom; d < rangeTo; d = d.AddDays(1))
            {
                var list = events.Where(e => e.Start.Date == d).OrderBy(e => e.Start).ToList();
                var cell = new CalendarDay
                {
                    Iso = Iso(d), Dow = Dow[(int)d.DayOfWeek], Day = d.Day, IsToday = d == today,
                    InMonth = view != "month" || d.Month == focus.Month,
                    AllDay = list.Where(e => e.IsAllDay).ToList(),
                    Events = list.Where(e => !e.IsAllDay).ToList()
                };
                if (view != "month") Layout(cell.Events, vm.StartHour, vm.EndHour);
                vm.Days.Add(cell);
            }
            return View(vm);
        }

        // GET /Time/Events?status=&range=&type=&source=&q=
        public async Task<IActionResult> Events(string? status, string? range, string? type, string? source, string? q)
        {
            Page("Sự kiện", "TimeEvents");
            var uid = User.GetUserId();
            var all = await LoadEventsAsync(uid, null, null);
            var today = DateTime.Today;

            status = status is "upcoming" or "ongoing" or "done" or "cancelled" ? status : "all";
            range = range is "today" or "week" or "month" ? range : "all";
            source = source != null && Sources.ContainsKey(source) ? source : "all";
            type = string.IsNullOrWhiteSpace(type) ? "all" : type;

            IEnumerable<TimeEventItem> query = all;
            if (status != "all") query = query.Where(e => e.Status == status);
            if (range == "today") query = query.Where(e => e.Start.Date == today);
            if (range == "week") query = query.Where(e => e.Start.Date >= WeekStart(today) && e.Start.Date < WeekStart(today).AddDays(7));
            if (range == "month") query = query.Where(e => e.Start.Year == today.Year && e.Start.Month == today.Month);
            if (type != "all") query = query.Where(e => e.TypeLabel == type);
            if (source != "all") query = query.Where(e => e.Source == source);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(e => e.Title.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase) || e.Location.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase));

            return View(new TimeEventsViewModel
            {
                Status = status, Range = range, Type = type, Source = source, Q = q,
                Total = all.Count,
                Upcoming = all.Count(e => e.Status == "upcoming"),
                Done = all.Count(e => e.Status == "done"),
                Cancelled = all.Count(e => e.Status == "cancelled"),
                Items = query.OrderByDescending(e => e.Start).Take(300).ToList()
            });
        }

        // GET /Time/Pomodoro
        public async Task<IActionResult> Pomodoro()
        {
            Page("Pomodoro", "TimePomodoro");
            var uid = User.GetUserId();
            var today = DateTime.Today;
            var ws = WeekStart(today);

            var done = await _context.PomodoroSessions.AsNoTracking()
                .Where(p => p.UserId == uid && p.IsCompleted && p.CompletedAt != null)
                .OrderByDescending(p => p.CompletedAt).Take(200).ToListAsync();
            var tasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == uid && t.Status != "done" && t.Status != "completed")
                .OrderBy(t => t.Deadline == null).ThenBy(t => t.Deadline).Take(40)
                .Select(t => new { t.Id, t.Title }).ToListAsync();
            var courses = await _context.Courses.AsNoTracking().Where(c => c.UserId == uid).OrderBy(c => c.CourseName)
                .Select(c => new { c.Id, c.CourseName }).ToListAsync();

            var taskNames = tasks.ToDictionary(t => t.Id, t => t.Title);
            var courseNames = courses.ToDictionary(c => c.Id, c => c.CourseName);

            var targets = new List<PomodoroTarget> { new() { Value = "", Label = "Tập trung tự do (không gắn công việc)" } };
            targets.AddRange(tasks.Select(t => new PomodoroTarget { Value = "task:" + t.Id, Label = "Công việc: " + t.Title }));
            targets.AddRange(courses.Select(c => new PomodoroTarget { Value = "course:" + c.Id, Label = "Môn học: " + c.CourseName }));

            string NameOf(PomodoroSession p) =>
                p.TaskId.HasValue ? (taskNames.TryGetValue(p.TaskId.Value, out var tn) ? "Công việc: " + tn : "Công việc")
                : p.CourseId.HasValue ? (courseNames.TryGetValue(p.CourseId.Value, out var cn) ? "Môn học: " + cn : "Môn học")
                : "Tập trung tự do";

            var todayList = done.Where(p => p.CompletedAt!.Value.Date == today).ToList();
            var weekList = done.Where(p => p.CompletedAt!.Value.Date >= ws).ToList();
            return View(new TimePomodoroViewModel
            {
                Targets = targets,
                History = done.Take(30).Select(p => new PomodoroSessionHistory
                {
                    Id = p.Id, TaskOrCourseName = NameOf(p), DurationMinutes = p.DurationMinutes,
                    CompletedLabel = p.CompletedAt!.Value.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)
                }).ToList(),
                TodaySessions = todayList.Count, TodayText = Minutes(todayList.Sum(p => p.DurationMinutes)),
                WeekSessions = weekList.Count, WeekText = Minutes(weekList.Sum(p => p.DurationMinutes)),
                TotalSessions = done.Count
            });
        }

        // ================================================================== Hàm hỗ trợ

        private async Task<List<TimeEventItem>> LoadEventsAsync(int uid, DateTime? rangeFrom, DateTime? rangeTo)
        {
            var q = _context.Events.AsNoTracking().Where(e => e.UserId == uid);
            if (rangeFrom.HasValue) q = q.Where(e => e.StartTime >= rangeFrom.Value);
            if (rangeTo.HasValue) q = q.Where(e => e.StartTime < rangeTo.Value);
            var rows = await q.OrderBy(e => e.StartTime).ToListAsync();
            var now = DateTime.Now;
            return rows.Select(e => Map(e, now)).ToList();
        }

        /// <summary>
        /// Giờ kết thúc. Event.EndTime hiện chỉ là DateOnly (không có giờ), nên tạm tính: sự kiện nhiều ngày kết thúc
        /// 23:59 ngày cuối, còn lại mặc định kéo dài 1 giờ. KHI BACK-END ĐỔI EndTime SANG DateTime: trả thẳng giá trị đó.
        /// </summary>
        private static DateTime ResolveEnd(Event e)
        {
            var endDate = e.EndTime.ToDateTime(TimeOnly.MinValue);
            if (e.IsAllDay) return e.StartTime.Date.AddDays(1).AddMinutes(-1);
            if (endDate > e.StartTime.Date) return endDate.AddDays(1).AddMinutes(-1);
            return e.StartTime.AddHours(1);
        }

        private static string ResolveSource(Event e)
        {
            var s = (e.SourceType ?? "").Trim().ToLowerInvariant();
            if (s == "") return "personal";
            if (s.Contains("course") || s.Contains("study") || s.Contains("schedule") || s.Contains("assign") || s.Contains("học")) return "study";
            if (s.Contains("task") || s.Contains("deadline") || s.Contains("công việc")) return "task";
            if (s.Contains("project") || s.Contains("dự án")) return "project";
            if (s.Contains("life") || s.Contains("sinh hoạt")) return "life";
            return "personal";
        }

        private TimeEventItem Map(Event e, DateTime now)
        {
            var src = ResolveSource(e);
            var info = Sources[src];
            var editable = string.IsNullOrWhiteSpace(e.SourceType);
            var end = ResolveEnd(e);
            var rule = (e.RecurrenceRule ?? "").Trim().ToLowerInvariant();
            var (recKey, recLabel) = rule switch
            {
                "" or "none" => ("none", "Không lặp"),
                var r when r.Contains("daily") => ("daily", "Hằng ngày"),
                var r when r.Contains("weekly") => ("weekly", "Hằng tuần"),
                var r when r.Contains("monthly") => ("monthly", "Hằng tháng"),
                _ => ("custom", "Tùy chỉnh")
            };

            return new TimeEventItem
            {
                Id = e.Id,
                Title = e.Title,
                Source = src,
                SourceLabel = info.Label,
                TypeLabel = string.IsNullOrWhiteSpace(e.EventType) ? info.Label : e.EventType,
                Color = editable && e.Color is { Length: 7 } c && c[0] == '#' && c.Skip(1).All(Uri.IsHexDigit) ? c : info.Color,
                Start = e.StartTime,
                End = end,
                IsAllDay = e.IsAllDay,
                Location = e.Location ?? "",
                Description = e.Description ?? "",
                Reminder = "",                                    // Event chưa có cột nhắc nhở
                Recurrence = recLabel,
                RecurrenceKey = recKey,
                OriginLabel = info.Origin,
                OriginUrl = editable || info.Controller == null ? "" : Url.Action(info.Action, info.Controller) ?? "",
                Editable = editable,
                // Entity chưa có trạng thái hủy, nên trạng thái chỉ suy ra từ thời gian.
                Status = e.IsAllDay ? (e.StartTime.Date < now.Date ? "done" : e.StartTime.Date == now.Date ? "ongoing" : "upcoming")
                                    : (end <= now ? "done" : e.StartTime <= now ? "ongoing" : "upcoming")
            };
        }

        /// <summary>Gán vị trí trên lưới giờ và chia làn cho các sự kiện trùng giờ trong cùng một ngày.</summary>
        private static void Layout(List<TimeEventItem> day, int startHour, int endHour)
        {
            double total = (endHour - startHour) * 60.0;
            foreach (var e in day)
            {
                var s = Math.Clamp((e.Start - e.Start.Date).TotalMinutes - startHour * 60, 0, total);
                var en = Math.Clamp((e.End.Date > e.Start.Date ? endHour * 60 : (e.End - e.End.Date).TotalMinutes) - startHour * 60, 0, total);
                e.TopPct = Math.Round(s / total * 100, 2);
                e.HeightPct = Math.Round(Math.Max(en - s, 30) / total * 100, 2);
            }

            // Gom thành cụm trùng giờ, mỗi cụm chia làn theo thứ tự bắt đầu
            var cluster = new List<TimeEventItem>();
            DateTime clusterEnd = DateTime.MinValue;
            void Flush()
            {
                if (cluster.Count == 0) return;
                var laneEnds = new List<DateTime>();
                foreach (var e in cluster)
                {
                    var lane = laneEnds.FindIndex(x => x <= e.Start);
                    if (lane < 0) { laneEnds.Add(e.End); lane = laneEnds.Count - 1; } else laneEnds[lane] = e.End;
                    e.Lane = lane;
                }
                foreach (var e in cluster) e.Lanes = laneEnds.Count;
                cluster.Clear();
            }
            foreach (var e in day.OrderBy(x => x.Start))
            {
                if (cluster.Count > 0 && e.Start >= clusterEnd) { Flush(); clusterEnd = DateTime.MinValue; }
                cluster.Add(e);
                if (e.End > clusterEnd) clusterEnd = e.End;
            }
            Flush();
        }
    }
}
