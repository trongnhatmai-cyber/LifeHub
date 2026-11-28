using System.Globalization;
using System.Text;
using LifeHub.BusinessLogicLayer.ReportManagement;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.ReportManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

/// <summary>
/// Thống kê & báo cáo: 6 màn hình (Tổng quan, Học tập, Công việc & dự án, Tài chính, Mục tiêu & thói quen, Sinh hoạt).
/// Mọi truy vấn đều giới hạn theo người dùng hiện tại. Controller tính KPI/biểu đồ/bảng vào ViewModel, View chỉ in ra.
/// Bộ lọc thời gian (range, from, to, compare) áp dụng đồng nhất cho mọi KPI và biểu đồ của trang.
/// </summary>
[Authorize]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    public ReportController(ApplicationDbContext context) => _context = context;

    private static readonly CultureInfo Vi = new("vi-VN");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // Màu phân hệ nguồn (khớp --pastel-*-accent trong layout.css)
    private const string CStudy = "#2563eb", CTask = "#ea580c", CProject = "#16a34a", CTime = "#0d9488",
                         CGoal = "#ca8a04", CHabit = "#4f46e5", CFinance = "#e11d48", CLife = "#059669", CViolet = "#9333ea";

    // ================================================================== 6 MÀN HÌNH

    [HttpGet] public async Task<IActionResult> Index(string? range, string? from, string? to, bool? compare) => View("Index", await BuildAsync("overview", range, from, to, compare));
    [HttpGet] public async Task<IActionResult> Study(string? range, string? from, string? to, bool? compare) => View("Study", await BuildAsync("study", range, from, to, compare));
    [HttpGet] public async Task<IActionResult> Tasks(string? range, string? from, string? to, bool? compare) => View("Tasks", await BuildAsync("tasks", range, from, to, compare));
    [HttpGet] public async Task<IActionResult> Finance(string? range, string? from, string? to, bool? compare) => View("Finance", await BuildAsync("finance", range, from, to, compare));
    [HttpGet] public async Task<IActionResult> Productivity(string? range, string? from, string? to, bool? compare, string? goalStatus, string? goalType) => View("Productivity", await BuildAsync("goals", range, from, to, compare, goalStatus, goalType));
    [HttpGet] public async Task<IActionResult> Lifestyle(string? range, string? from, string? to, bool? compare) => View("Lifestyle", await BuildAsync("lifestyle", range, from, to, compare));
    [HttpGet] public async Task<IActionResult> Pomodoro(string? range, string? from, string? to, bool? compare) => View("Pomodoro", await BuildAsync("pomodoro", range, from, to, compare));

    /// <summary>Xuất các KPI và nhận xét của một báo cáo ra CSV (mở được bằng Excel). In / lưu PDF dùng chức năng in của trình duyệt.</summary>
    [HttpGet]
    public async Task<IActionResult> Export(string section, string? range, string? from, string? to, bool? compare)
    {
        var vm = await BuildAsync(section, range, from, to, compare);
        string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        var sb = new StringBuilder();
        sb.AppendLine($"{Q("Báo cáo")},{Q(vm.Title)}");
        sb.AppendLine($"{Q("Khoảng thời gian")},{Q(vm.Range.Text)}");
        sb.AppendLine();
        sb.AppendLine($"{Q("Chỉ số")},{Q("Giá trị")},{Q("Đơn vị")},{Q("Ghi chú")},{Q("So với kỳ trước")}");
        foreach (var k in vm.Kpis) sb.AppendLine($"{Q(k.Title)},{Q(k.Value)},{Q(k.Unit)},{Q(k.Note)},{Q(k.Delta ?? "")}");
        foreach (var t in vm.Tables)
        {
            sb.AppendLine(); sb.AppendLine(Q(t.Title));
            sb.AppendLine(string.Join(",", t.Headers.Select(Q)));
            foreach (var row in t.Rows) sb.AppendLine(string.Join(",", row.Cells.Select(c => Q(c.Text))));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"bao-cao-{vm.Section}-{vm.Range.FromIso}-{vm.Range.ToIso}.csv");
    }

    private async Task<ReportBaseViewModel> BuildAsync(string section, string? range, string? from, string? to, bool? compare, string? goalStatus = null, string? goalType = null)
    {
        var r = ReportRange.Parse(range, from, to, compare ?? true, DateOnly.FromDateTime(DateTime.Today));
        var uid = User.GetUserId();
        var vm = section switch
        {
            "study" => await StudyAsync(uid, r),
            "tasks" => await TasksAsync(uid, r),
            "finance" => await FinanceAsync(uid, r),
            "goals" => await GoalsHabitsAsync(uid, r, goalStatus, goalType),
            "lifestyle" => await LifestyleAsync(uid, r),
            "pomodoro" => await PomodoroAsync(uid, r),
            _ => await OverviewAsync(uid, r)
        };
        vm.Range = r;
        return vm;
    }

    // ================================================================== A. TỔNG QUAN HOẠT ĐỘNG

    private async Task<ReportBaseViewModel> OverviewAsync(int uid, ReportRange r)
    {
        var fromDt = r.FromDt; var toDt = r.ToExclusive; var pFrom = r.PrevFromDt; var pTo = r.PrevToExclusive;
        var scope = _context.Tasks.AsNoTracking().Where(t => t.UserId == uid || t.AssignedTo == uid);
        var done = await scope.Where(t => t.CompletedAt >= fromDt && t.CompletedAt < toDt).Select(t => new { t.Title, t.ProjectId, When = t.CompletedAt!.Value }).ToListAsync();
        var prevDone = await scope.CountAsync(t => t.CompletedAt >= pFrom && t.CompletedAt < pTo);
        var anyTask = await scope.AnyAsync();

        var sessions = await _context.PomodoroSessions.AsNoTracking()
            .Where(p => p.UserId == uid && p.IsCompleted && p.StartedAt >= fromDt && p.StartedAt < toDt)
            .Select(p => new { p.StartedAt, p.DurationMinutes }).ToListAsync();
        var prevFocus = await _context.PomodoroSessions.Where(p => p.UserId == uid && p.IsCompleted && p.StartedAt >= pFrom && p.StartedAt < pTo).SumAsync(p => (int?)p.DurationMinutes) ?? 0;
        var anySession = await _context.PomodoroSessions.AnyAsync(p => p.UserId == uid);

        var habits = await _context.Habits.AsNoTracking().Where(h => h.UserId == uid && h.Active).ToListAsync();
        var hIds = habits.Select(h => h.Id).ToList();
        var logs = await _context.HabitLogs.AsNoTracking().Where(l => hIds.Contains(l.HabitId) && l.CheckDate >= r.PrevFrom && l.CheckDate <= r.To).ToListAsync();
        var byHabit = logs.ToLookup(l => l.HabitId);
        int hExp = 0, hDone = 0, pExp = 0, pDone = 0;
        foreach (var h in habits)
        {
            var c = ReportMath.HabitPeriod(h, byHabit[h.Id], r.From, r.To, r.Today); hExp += c.Expected; hDone += c.Done;
            var p = ReportMath.HabitPeriod(h, byHabit[h.Id], r.PrevFrom, r.PrevTo, r.Today); pExp += p.Expected; pDone += p.Done;
        }
        var doneLogs = logs.Where(l => l.IsCompleted && l.CheckDate >= r.From).ToList();

        var goals = await _context.Goals.AsNoTracking().Where(g => g.UserId == uid).ToListAsync();
        var liveGoals = goals.Where(g => !IsOneOf(g.Status, "cancelled", "archived")).ToList();
        var goalsCreated = goals.Count(g => g.CreatedAt >= fromDt && g.CreatedAt < toDt);

        var memberIds = await _context.ProjectMembers.Where(m => m.UserId == uid).Select(m => m.ProjectId).ToListAsync();
        var projects = await _context.Projects.AsNoTracking().Where(p => p.CreatedBy == uid || memberIds.Contains(p.Id)).ToListAsync();
        var activeProjects = projects.Count(p => !IsOneOf(p.Status, "completed", "cancelled", "archived", "done"));

        var courseIds = await _context.Courses.Where(c => c.UserId == uid).Select(c => c.Id).ToListAsync();
        var submitted = await _context.Assignments.CountAsync(a => courseIds.Contains(a.CourseId) && a.IsSubmitted && a.DueDate >= fromDt && a.DueDate < toDt);
        var lifeLogs = await _context.LifestyleLogs.AsNoTracking().Where(l => l.UserId == uid && l.LogDate >= r.From && l.LogDate <= r.To).Select(l => l.LogDate).ToListAsync();

        var vm = Base("overview", "Index", "Tổng quan hoạt động", "Theo dõi tiến trình và nhìn lại những gì bạn đã đạt được.", CViolet);

        // KPI
        var focusMin = sessions.Sum(s => s.DurationMinutes);
        var kTask = new ReportKpi { Badge = "TASKS", Title = "Công việc hoàn thành", Value = anyTask ? done.Count.ToString() : "--", Unit = anyTask ? "công việc" : "", Note = r.NoteText, LinkAction = "Tasks",
            Tooltip = "Số công việc có CompletedAt nằm trong khoảng thời gian đã chọn (công việc bạn tạo hoặc được giao)." };
        if (anyTask) Delta(kTask, done.Count, prevDone, r);
        var kProj = new ReportKpi { Badge = "PROJECTS", Title = "Dự án đang hoạt động", Value = projects.Count > 0 ? activeProjects.ToString() : "--", Unit = projects.Count > 0 ? "dự án" : "", Note = "Tại thời điểm báo cáo", LinkAction = "Tasks",
            Tooltip = "Dự án bạn tạo hoặc tham gia, có trạng thái chưa hoàn thành, chưa hủy, chưa lưu trữ." };
        var avgGoal = liveGoals.Count == 0 ? (double?)null : liveGoals.Average(g => g.ProgressPercent);
        var kGoal = new ReportKpi { Badge = "GOALS", Title = "Tiến độ mục tiêu", Value = avgGoal.HasValue ? Math.Round(avgGoal.Value).ToString("0") : "--", Unit = avgGoal.HasValue ? "%" : "", Note = "Trung bình tại thời điểm báo cáo", LinkAction = "Productivity",
            Tooltip = "Trung bình ProgressPercent của các mục tiêu chưa hủy, chưa lưu trữ." };
        var kHabit = new ReportKpi { Badge = "HABITS", Title = "Tỷ lệ duy trì thói quen", Value = hExp > 0 ? ReportMath.Pct(hDone, hExp).ToString() : "--", Unit = hExp > 0 ? "%" : "", Note = $"{hDone}/{hExp} lần, {r.NoteText}", LinkAction = "Productivity",
            Tooltip = "Số lần hoàn thành / số lần dự kiến theo lịch của từng thói quen đang hoạt động, chỉ tính từ ngày thói quen bắt đầu." };
        if (hExp > 0 && pExp > 0) Delta(kHabit, ReportMath.Pct(hDone, hExp), ReportMath.Pct(pDone, pExp), r, points: true);
        var kFocus = new ReportKpi { Badge = "FOCUS", Title = "Thời gian tập trung", Value = anySession ? Hours(focusMin) : "--", Unit = anySession ? "giờ" : "", Note = $"{sessions.Count} phiên, {r.NoteText}",
            Tooltip = "Tổng DurationMinutes của các phiên Pomodoro đã hoàn thành, bắt đầu trong khoảng thời gian đã chọn." };
        if (anySession) Delta(kFocus, focusMin, prevFocus, r);
        vm.Kpis.AddRange(new[] { kTask, kProj, kGoal, kHabit, kFocus });

        // Biểu đồ hoạt động theo thời gian (3 chuỗi, đơn vị đều là "số lần", bật/tắt từng chuỗi)
        var buckets = r.Buckets();
        List<double?> Series(IEnumerable<DateOnly> dates) => buckets.Select(b => (double?)dates.Count(d => d >= b.Start && d <= b.End)).ToList();
        vm.Charts.Add(new ReportChartCard
        {
            Id = "activity", Title = "Hoạt động theo thời gian", Subtitle = $"Số lần ghi nhận theo {r.BucketUnit}. Bấm tên chuỗi để bật/tắt.", Type = "line", Wide = true, Toggleable = true,
            Data = new ChartData
            {
                Title = "Hoạt động theo thời gian", Labels = buckets.Select(b => b.Label).ToList(), Kind = "int",
                Series =
                {
                    new ChartSeries { Name = "Công việc hoàn thành", Color = CTask, Values = Series(done.Select(d => DateOnly.FromDateTime(d.When))) },
                    new ChartSeries { Name = "Phiên Pomodoro", Color = CTime, Values = Series(sessions.Select(s => DateOnly.FromDateTime(s.StartedAt))) },
                    new ChartSeries { Name = "Thói quen hoàn thành", Color = CHabit, Values = Series(doneLogs.Select(l => l.CheckDate)) }
                }
            }
        });

        // Phân bổ theo phân hệ: cùng một đơn vị đo là "số hoạt động ghi nhận" trong kỳ
        var slices = new List<DonutSlice>();
        void Add(string label, string color, int n) { if (n > 0) slices.Add(new DonutSlice { Label = label, Color = color, Value = n, ValueText = n + " hoạt động" }); }
        Add("Học tập", CStudy, submitted);
        Add("Công việc", CTask, done.Count(d => d.ProjectId == null));
        Add("Dự án", CProject, done.Count(d => d.ProjectId != null));
        Add("Mục tiêu", CGoal, goalsCreated);
        Add("Thói quen", CHabit, doneLogs.Count);
        Add("Sinh hoạt", CLife, lifeLogs.Count);
        vm.Charts.Add(new ReportChartCard { Id = "alloc", Title = "Phân bổ hoạt động theo phân hệ", Subtitle = "Tỷ trọng theo số hoạt động ghi nhận trong kỳ", Type = "donut", Slices = slices, CenterTop = slices.Sum(s => s.Value).ToString("0"), CenterBottom = "hoạt động" });

        // Nhận xét (quy tắc xác định, chưa dùng AI)
        if (anyTask && prevDone > 0 && r.Compare)
        {
            var diff = ReportMath.Pct(done.Count - prevDone, prevDone);
            vm.Insights.Add(diff > 0 ? $"Số công việc hoàn thành trong kỳ tăng {diff}% so với kỳ trước ({done.Count} so với {prevDone})."
                : diff < 0 ? $"Số công việc hoàn thành trong kỳ giảm {-diff}% so với kỳ trước ({done.Count} so với {prevDone})."
                : $"Số công việc hoàn thành giữ nguyên so với kỳ trước ({done.Count}).");
        }
        if (sessions.Count >= 3)
        {
            string Part(int h) => h is >= 5 and < 12 ? "buổi sáng" : h is >= 12 and < 18 ? "buổi chiều" : h is >= 18 and < 23 ? "buổi tối" : "ban đêm";
            var top = sessions.GroupBy(s => Part(s.StartedAt.Hour)).Select(g => new { g.Key, Min = g.Sum(x => x.DurationMinutes) }).OrderByDescending(x => x.Min).First();
            if (top.Min * 100 >= focusMin * 40) vm.Insights.Add($"Thời gian tập trung được ghi nhận nhiều nhất vào {top.Key} ({Hours(top.Min)} giờ).");
        }
        var lowGoals = liveGoals.Count(g => !g.IsCompleted && g.ProgressPercent < 25);
        if (lowGoals > 0) vm.Insights.Add($"Có {lowGoals} mục tiêu đang hoạt động còn dưới 25% tiến độ.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét. Hãy ghi nhận thêm hoạt động trong kỳ này.");

        // Hoạt động gần đây
        var recent = new List<(DateTime When, ReportRow Row)>();
        ReportRow Row(DateTime when, string text, string module, string color, string status, string cls, string? url) => new()
        {
            Cells = { ReportCell.T(when.ToString("dd/MM/yyyy", Inv)), ReportCell.T(text), new ReportCell { Text = module, Color = color }, new ReportCell { Text = status, Status = cls }, new ReportCell { Text = url == null ? "" : "Xem", Url = url, Right = true } }
        };
        foreach (var d in done.OrderByDescending(x => x.When).Take(8))
            recent.Add((d.When, Row(d.When, "Hoàn thành công việc: " + d.Title, d.ProjectId == null ? "Công việc" : "Dự án", d.ProjectId == null ? CTask : CProject, "Hoàn thành", "st-graded", Url.Action("Index", "Task"))));
        foreach (var s in sessions.OrderByDescending(x => x.StartedAt).Take(8))
            recent.Add((s.StartedAt, Row(s.StartedAt, $"Phiên tập trung {s.DurationMinutes} phút", "Thời gian", CTime, "Hoàn thành", "st-graded", Url.Action("Pomodoro", "Time"))));
        var habitNames = habits.ToDictionary(h => h.Id, h => h.Title);
        foreach (var l in doneLogs.OrderByDescending(x => x.CheckDate).Take(8))
        { var when = l.CheckDate.ToDateTime(TimeOnly.MinValue); recent.Add((when, Row(when, "Check-in thói quen: " + habitNames.GetValueOrDefault(l.HabitId, "Thói quen"), "Thói quen", CHabit, "Hoàn thành", "st-graded", Url.Action("HabitTracking", "Habit")))); }
        foreach (var d in lifeLogs.OrderByDescending(x => x).Take(5))
        { var when = d.ToDateTime(TimeOnly.MinValue); recent.Add((when, Row(when, "Ghi nhận sinh hoạt", "Sinh hoạt", CLife, "Đã ghi nhận", "st-submitted", Url.Action("Index", "Lifestyle")))); }
        vm.Tables.Add(new ReportTable
        {
            Title = "Hoạt động gần đây", Subtitle = "Các bản ghi mới nhất trong kỳ", Headers = { "NGÀY", "HOẠT ĐỘNG", "PHÂN HỆ", "TRẠNG THÁI", "CHI TIẾT" }, RightColumns = { 4 },
            Rows = recent.OrderByDescending(x => x.When).Take(14).Select(x => x.Row).ToList(), PreviewRows = 6, Empty = "Chưa có hoạt động nào trong khoảng thời gian này."
        });
        vm.Notes.Add("Hoạt động là một bản ghi có ngày thuộc kỳ: công việc hoàn thành (CompletedAt), phiên Pomodoro hoàn thành, lần check-in thói quen hoàn thành, ghi nhận sinh hoạt, bài tập đã nộp có hạn nộp trong kỳ và mục tiêu mới tạo. Không cộng số giờ với số công việc.");
        vm.Notes.Add("Chưa có chuỗi \"Mục tiêu được cập nhật\" vì bảng mục tiêu chưa lưu thời điểm cập nhật.");
        return vm;
    }

    // ================================================================== B. HỌC TẬP

    private async Task<ReportBaseViewModel> StudyAsync(int uid, ReportRange r)
    {
        var fromDt = r.FromDt; var toDt = r.ToExclusive; var now = DateTime.Now;
        var courses = await _context.Courses.AsNoTracking().Where(c => c.UserId == uid).OrderBy(c => c.CourseCode).ToListAsync();
        var ids = courses.Select(c => c.Id).ToList();
        var all = await _context.Assignments.AsNoTracking().Where(a => ids.Contains(a.CourseId)).ToListAsync();
        var inRange = all.Where(a => a.DueDate >= fromDt && a.DueDate < toDt).ToList();
        var prevSubmitted = all.Count(a => a.IsSubmitted && a.DueDate >= r.PrevFromDt && a.DueDate < r.PrevToExclusive);
        var courseName = courses.ToDictionary(c => c.Id, c => c.CourseName);
        var vm = Base("study", "Study", "Báo cáo học tập", "Theo dõi kết quả và tiến độ học tập của bạn.", CStudy);

        var scored = courses.Where(c => c.FinalScore.HasValue).ToList();
        var active = courses.Count(c => !IsOneOf(c.Status, "completed", "dropped", "cancelled"));
        var kSub = new ReportKpi { Badge = "SUBMITTED", Title = "Bài tập đã nộp", Value = all.Count > 0 ? inRange.Count(a => a.IsSubmitted).ToString() : "--", Unit = all.Count > 0 ? "bài" : "", Note = r.NoteText, Tooltip = "Số bài tập đã nộp có hạn nộp nằm trong khoảng thời gian đã chọn." };
        if (all.Count > 0) Delta(kSub, inRange.Count(a => a.IsSubmitted), prevSubmitted, r);
        vm.Kpis.Add(new ReportKpi { Badge = "SCORE", Title = "Điểm trung bình", Value = scored.Count > 0 ? scored.Average(c => c.FinalScore!.Value).ToString("0.0", Inv) : "--", Note = scored.Count > 0 ? $"{scored.Count} môn đã có điểm cuối kỳ" : "Chưa có môn nào có điểm",
            Tooltip = "Trung bình cộng điểm cuối kỳ của các môn đã có điểm, giữ nguyên thang điểm đã nhập. Chưa quy đổi GPA vì chưa thống nhất công thức và cách xử lý môn chưa có điểm." });
        vm.Kpis.Add(new ReportKpi { Badge = "COURSES", Title = "Môn đang học", Value = courses.Count > 0 ? active.ToString() : "--", Unit = courses.Count > 0 ? "môn" : "", Note = "Tại thời điểm báo cáo", Tooltip = "Số môn có trạng thái chưa hoàn thành." });
        vm.Kpis.Add(kSub);
        vm.Kpis.Add(new ReportKpi { Badge = "REMAINING", Title = "Bài tập còn lại", Value = all.Count > 0 ? all.Count(a => !a.IsSubmitted && a.DueDate >= now).ToString() : "--", Unit = all.Count > 0 ? "bài" : "", Note = "Chưa nộp, chưa đến hạn", Tooltip = "Bài tập chưa nộp và hạn nộp còn ở phía trước (tại thời điểm báo cáo)." });

        var shown = courses.Where(c => c.FinalScore.HasValue || c.TargetScore.HasValue).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "scores", Title = "Điểm theo từng môn", Subtitle = "Điểm hiện tại so với điểm mục tiêu", Type = "bar", Wide = true,
            Empty = "Chưa có môn nào có điểm hoặc điểm mục tiêu.",
            Data = new ChartData { Title = "Điểm theo môn", Kind = "dec1", Labels = shown.Select(c => c.CourseCode).ToList(), Series = {
                new ChartSeries { Name = "Điểm hiện tại", Color = CStudy, Values = shown.Select(c => (double?)(c.FinalScore.HasValue ? (double)c.FinalScore.Value : null)).ToList() },
                new ChartSeries { Name = "Điểm mục tiêu", Color = "#93c5fd", Values = shown.Select(c => (double?)(c.TargetScore.HasValue ? (double)c.TargetScore.Value : null)).ToList() } } } });

        // Phân bố điểm bài tập theo % điểm tối đa (MaxScore trống thì coi là 10)
        var pcts = inRange.Where(a => a.Score.HasValue).Select(a => (double)a.Score!.Value / (double)(a.MaxScore is > 0 ? a.MaxScore.Value : 10m) * 100).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "dist", Title = "Phân bố điểm bài tập", Subtitle = "Số bài theo tỷ lệ điểm đạt được", Type = "bar", Empty = "Chưa có bài tập nào được ghi điểm trong kỳ.",
            Data = new ChartData { Title = "Phân bố điểm bài tập", Kind = "int", Labels = { "Dưới 50%", "50–69%", "70–84%", "85–100%" }, Series = { new ChartSeries { Name = "Số bài", Color = CStudy, Values = {
                pcts.Count(p => p < 50), pcts.Count(p => p >= 50 && p < 70), pcts.Count(p => p >= 70 && p < 85), pcts.Count(p => p >= 85) } } } } });
        var lateCount = inRange.Count(a => !a.IsSubmitted && a.DueDate < now);
        vm.Charts.Add(new ReportChartCard { Id = "status", Title = "Tình trạng bài tập", Subtitle = r.NoteText, Type = "donut",
            Slices = { new DonutSlice { Label = "Đã nộp", Color = CProject, Value = inRange.Count(a => a.IsSubmitted), ValueText = inRange.Count(a => a.IsSubmitted) + " bài" },
                       new DonutSlice { Label = "Chưa nộp", Color = "#94a3b8", Value = inRange.Count(a => !a.IsSubmitted && a.DueDate >= now), ValueText = inRange.Count(a => !a.IsSubmitted && a.DueDate >= now) + " bài" },
                       new DonutSlice { Label = "Quá hạn", Color = "#dc2626", Value = lateCount, ValueText = lateCount + " bài" } },
            CenterTop = inRange.Count.ToString(), CenterBottom = "bài tập", Empty = "Không có bài tập nào có hạn nộp trong kỳ." });

        vm.Tables.Add(new ReportTable { Title = "Tiến độ môn học", Subtitle = "Tất cả môn của bạn", Headers = { "MÔN HỌC", "TÍN CHỈ", "ĐIỂM HIỆN TẠI", "ĐIỂM MỤC TIÊU", "TRẠNG THÁI" }, RightColumns = { 1, 2, 3 }, Empty = "Chưa có môn học nào. Hãy thêm môn ở phân hệ Học tập.",
            Rows = courses.Select(c => new ReportRow { Cells = {
                new ReportCell { Text = c.CourseName, Sub = c.CourseCode, Color = c.Color ?? CStudy }, ReportCell.T(c.Credits?.ToString() ?? "--"), ReportCell.T(c.FinalScore?.ToString("0.0", Inv) ?? "Chưa có"), ReportCell.T(c.TargetScore?.ToString("0.0", Inv) ?? "--"),
                IsOneOf(c.Status, "completed") ? new ReportCell { Text = "Đã hoàn thành", Status = "st-graded" } : new ReportCell { Text = "Đang học", Status = "st-submitted" } } }).ToList() });

        vm.Tables.Add(new ReportTable { Title = "Bài tập trong kỳ", Subtitle = "Theo hạn nộp trong khoảng thời gian đã chọn", Headers = { "BÀI TẬP", "HẠN NỘP", "TRẠNG THÁI", "ĐIỂM" }, RightColumns = { 3 }, PreviewRows = 8, Empty = "Không có bài tập nào có hạn nộp trong kỳ.",
            Rows = inRange.OrderByDescending(a => a.DueDate).Select(a => new ReportRow { Cells = {
                ReportCell.T(a.Title, courseName.GetValueOrDefault(a.CourseId)), ReportCell.T(a.DueDate.ToString("dd/MM/yyyy", Inv)),
                a.IsSubmitted ? new ReportCell { Text = "Đã nộp", Status = "st-graded" } : a.DueDate < now ? new ReportCell { Text = "Quá hạn", Status = "st-overdue" } : new ReportCell { Text = "Chưa nộp", Status = "st-upcoming" },
                ReportCell.T(a.Score.HasValue ? $"{a.Score:0.#}/{(a.MaxScore ?? 10m):0.#}" : "--") } }).ToList() });

        if (r.Compare && prevSubmitted > 0 && all.Count > 0)
        { var cur = inRange.Count(a => a.IsSubmitted); vm.Insights.Add(cur >= prevSubmitted ? $"Bạn nộp {cur} bài tập trong kỳ, không thấp hơn kỳ trước ({prevSubmitted} bài)." : $"Số bài nộp trong kỳ là {cur}, thấp hơn kỳ trước ({prevSubmitted} bài)."); }
        if (lateCount > 0) vm.Insights.Add($"Có {lateCount} bài tập đã quá hạn mà chưa nộp trong kỳ này.");
        var below = courses.Count(c => c.FinalScore.HasValue && c.TargetScore.HasValue && c.FinalScore < c.TargetScore);
        if (below > 0) vm.Insights.Add($"{below} môn đang có điểm thấp hơn điểm mục tiêu.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét.");
        vm.Notes.Add("Không tự tính GPA: cần thống nhất công thức quy đổi và cách xử lý môn chưa có điểm. Điểm trung bình ở trên chỉ là trung bình cộng điểm cuối kỳ đã nhập.");
        vm.Notes.Add("Bảng môn học luôn hiển thị tất cả môn; các KPI bài tập, biểu đồ phân bố và bảng bài tập theo hạn nộp trong kỳ đã chọn.");
        return vm;
    }

    // ================================================================== C. CÔNG VIỆC & DỰ ÁN

    private async Task<ReportBaseViewModel> TasksAsync(int uid, ReportRange r)
    {
        var fromDt = r.FromDt; var toDt = r.ToExclusive; var now = DateTime.Now;
        var scope = _context.Tasks.AsNoTracking().Where(t => t.UserId == uid || t.AssignedTo == uid);
        var created = await scope.Where(t => t.CreatedAt >= fromDt && t.CreatedAt < toDt).ToListAsync();
        var completed = await scope.Where(t => t.CompletedAt >= fromDt && t.CompletedAt < toDt).ToListAsync();
        var prevCreated = await scope.CountAsync(t => t.CreatedAt >= r.PrevFromDt && t.CreatedAt < r.PrevToExclusive);
        var prevCompleted = await scope.CountAsync(t => t.CompletedAt >= r.PrevFromDt && t.CompletedAt < r.PrevToExclusive);
        var overdueNow = await scope.CountAsync(t => t.Status != "done" && t.Deadline != null && t.Deadline < now);
        var anyTask = await scope.AnyAsync();

        var memberIds = await _context.ProjectMembers.Where(m => m.UserId == uid).Select(m => m.ProjectId).ToListAsync();
        var projects = await _context.Projects.AsNoTracking().Where(p => p.CreatedBy == uid || memberIds.Contains(p.Id)).OrderBy(p => p.Name).ToListAsync();
        var activeProjects = projects.Count(p => !IsOneOf(p.Status, "completed", "cancelled", "archived", "done"));
        var vm = Base("tasks", "Tasks", "Báo cáo công việc & dự án", "Hiệu quả thực hiện công việc và tiến độ các dự án.", CTask);

        var withDeadline = completed.Where(t => t.Deadline.HasValue && t.CompletedAt.HasValue).ToList();
        var onTime = withDeadline.Count(t => t.CompletedAt <= t.Deadline);
        var kc = new ReportKpi { Badge = "CREATED", Title = "Công việc được tạo", Value = anyTask ? created.Count.ToString() : "--", Unit = anyTask ? "công việc" : "", Note = r.NoteText, Tooltip = "Số công việc có CreatedAt trong khoảng thời gian đã chọn." };
        var kd = new ReportKpi { Badge = "DONE", Title = "Công việc hoàn thành", Value = anyTask ? completed.Count.ToString() : "--", Unit = anyTask ? "công việc" : "", Note = r.NoteText, Tooltip = "Số công việc có CompletedAt trong khoảng thời gian đã chọn." };
        if (anyTask) { Delta(kc, created.Count, prevCreated, r); Delta(kd, completed.Count, prevCompleted, r); }
        vm.Kpis.Add(kc); vm.Kpis.Add(kd);
        vm.Kpis.Add(new ReportKpi { Badge = "OVERDUE", Title = "Công việc quá hạn", Value = anyTask ? overdueNow.ToString() : "--", Unit = anyTask ? "công việc" : "", Note = "Hiện đang quá hạn", Tooltip = "Công việc chưa hoàn thành có hạn chót đã qua, tại thời điểm báo cáo. Công việc quá hạn nhưng sau đó đã hoàn thành được tính là hoàn thành (và là hoàn thành trễ trong tỷ lệ đúng hạn)." });
        vm.Kpis.Add(new ReportKpi { Badge = "ON-TIME", Title = "Hoàn thành đúng hạn", Value = withDeadline.Count > 0 ? ReportMath.Pct(onTime, withDeadline.Count).ToString() : "--", Unit = withDeadline.Count > 0 ? "%" : "", Note = withDeadline.Count > 0 ? $"{onTime}/{withDeadline.Count} công việc có hạn chót" : "Chưa có công việc hoàn thành có hạn chót", Tooltip = "Trong các công việc hoàn thành trong kỳ và có hạn chót: tỷ lệ CompletedAt không muộn hơn Deadline." });
        vm.Kpis.Add(new ReportKpi { Badge = "PROJECTS", Title = "Dự án đang hoạt động", Value = projects.Count > 0 ? activeProjects.ToString() : "--", Unit = projects.Count > 0 ? "dự án" : "", Note = "Tại thời điểm báo cáo", Tooltip = "Dự án bạn tạo hoặc tham gia, chưa hoàn thành, chưa hủy, chưa lưu trữ." });

        // Cột chồng: công việc tạo trong từng cột thời gian, phân loại theo trạng thái HIỆN TẠI
        string Cls(LifeHub.Models.TaskManagement.Task t) => t.Status == "done" ? "done" : t.Deadline.HasValue && t.Deadline < now ? "overdue" : t.Status.Contains("progress", StringComparison.OrdinalIgnoreCase) || t.Status.Contains("doing", StringComparison.OrdinalIgnoreCase) ? "doing" : "todo";
        var buckets = r.Buckets();
        List<double?> Count(string cls) => buckets.Select(b => (double?)created.Count(t => DateOnly.FromDateTime(t.CreatedAt) >= b.Start && DateOnly.FromDateTime(t.CreatedAt) <= b.End && Cls(t) == cls)).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "taskstack", Title = "Công việc theo trạng thái", Subtitle = $"Công việc tạo trong mỗi {r.BucketUnit}, theo trạng thái hiện tại", Type = "bar", Wide = true, Toggleable = false,
            Data = new ChartData { Title = "Công việc theo trạng thái", Kind = "int", Stacked = true, Labels = buckets.Select(b => b.Label).ToList(), Series = {
                new ChartSeries { Name = "Hoàn thành", Color = CProject, Values = Count("done") }, new ChartSeries { Name = "Đang thực hiện", Color = CStudy, Values = Count("doing") },
                new ChartSeries { Name = "Chưa bắt đầu", Color = "#94a3b8", Values = Count("todo") }, new ChartSeries { Name = "Quá hạn", Color = "#dc2626", Values = Count("overdue") } } } });

        // Dự án
        var pIds = projects.Select(p => p.Id).ToList();
        var ptasks = await _context.Tasks.AsNoTracking().Where(t => t.ProjectId != null && pIds.Contains(t.ProjectId.Value)).Select(t => new { t.ProjectId, t.Status, t.EstimatedMinutes, t.Id }).ToListAsync();
        vm.Tables.Add(new ReportTable { Title = "Báo cáo dự án", Subtitle = "Các dự án bạn tạo hoặc tham gia", Headers = { "DỰ ÁN", "TIẾN ĐỘ", "BẮT ĐẦU", "DEADLINE", "TRẠNG THÁI", "CÔNG VIỆC" }, RightColumns = { 1, 5 }, Empty = "Chưa có dự án nào.",
            Rows = projects.Select(p => { var pt = ptasks.Where(t => t.ProjectId == p.Id).ToList(); var done = IsOneOf(p.Status, "completed", "done");
                return new ReportRow { Cells = { new ReportCell { Text = p.Name, Color = p.Color ?? CProject }, ReportCell.T(p.ProgressPercent + "%"), ReportCell.T(p.StartDate.ToString("dd/MM/yyyy", Inv)), ReportCell.T(p.DueDate?.ToString("dd/MM/yyyy", Inv) ?? "--"),
                    done ? new ReportCell { Text = "Hoàn thành", Status = "st-graded" } : IsOneOf(p.Status, "cancelled", "archived") ? new ReportCell { Text = "Đã đóng", Status = "st-rest" } : p.DueDate.HasValue && p.DueDate < r.Today ? new ReportCell { Text = "Quá hạn", Status = "st-overdue" } : new ReportCell { Text = "Đang hoạt động", Status = "st-submitted" },
                    ReportCell.T(pt.Count == 0 ? "--" : $"{pt.Count(t => t.Status == "done")}/{pt.Count}") } }; }).ToList() });

        // Thời gian: thực tế (Pomodoro) và ước tính (EstimatedMinutes) luôn ở hai cột riêng
        var sess = await _context.PomodoroSessions.AsNoTracking().Where(p => p.UserId == uid && p.IsCompleted && p.StartedAt >= fromDt && p.StartedAt < toDt).Select(p => new { p.TaskId, p.DurationMinutes }).ToListAsync();
        var sessTaskIds = sess.Where(s => s.TaskId.HasValue).Select(s => s.TaskId!.Value).Distinct().ToList();
        var taskProject = await _context.Tasks.AsNoTracking().Where(t => sessTaskIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.ProjectId);
        int? ProjOf(int? taskId) => taskId.HasValue && taskProject.TryGetValue(taskId.Value, out var pid) ? pid : null;
        var estCreated = created.Where(t => t.EstimatedMinutes.HasValue).ToList();
        var timeRows = new List<ReportRow>
        { new() { Cells = { ReportCell.T("Tất cả"), ReportCell.T(Hours(sess.Sum(s => s.DurationMinutes)) + " giờ"), ReportCell.T(estCreated.Count == 0 ? "--" : Hours(estCreated.Sum(t => t.EstimatedMinutes!.Value)) + " giờ") } } };
        foreach (var p in projects)
        {
            var actual = sess.Where(s => ProjOf(s.TaskId) == p.Id).Sum(s => s.DurationMinutes);
            var est = estCreated.Where(t => t.ProjectId == p.Id).Sum(t => t.EstimatedMinutes!.Value);
            if (actual > 0 || est > 0) timeRows.Add(new ReportRow { Cells = { new ReportCell { Text = p.Name, Color = p.Color ?? CProject }, ReportCell.T(actual > 0 ? Hours(actual) + " giờ" : "--"), ReportCell.T(est > 0 ? Hours(est) + " giờ" : "--") } });
        }
        vm.Tables.Add(new ReportTable { Title = "Phân tích thời gian", Subtitle = "Thời gian thực tế (phiên Pomodoro đã hoàn thành) và thời gian ước tính (công việc tạo trong kỳ) là hai chỉ số khác nhau", Headers = { "PHẠM VI", "THỰC TẾ (POMODORO)", "ƯỚC TÍNH (CÔNG VIỆC)" }, RightColumns = { 1, 2 }, Rows = timeRows });

        if (completed.Count > 0 && prevCompleted > 0 && r.Compare) vm.Insights.Add(completed.Count >= prevCompleted ? $"Bạn hoàn thành {completed.Count} công việc trong kỳ, không thấp hơn kỳ trước ({prevCompleted})." : $"Số công việc hoàn thành là {completed.Count}, thấp hơn kỳ trước ({prevCompleted}).");
        if (overdueNow > 0) vm.Insights.Add($"Hiện có {overdueNow} công việc đang quá hạn chưa hoàn thành.");
        if (withDeadline.Count >= 3) vm.Insights.Add($"{ReportMath.Pct(onTime, withDeadline.Count)}% công việc có hạn chót được hoàn thành đúng hạn trong kỳ.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét.");
        vm.Notes.Add("Biểu đồ phân loại theo trạng thái tại thời điểm báo cáo: công việc quá hạn nhưng đã hoàn thành nằm ở cột Hoàn thành; công việc chưa xong có hạn chót đã qua nằm ở cột Quá hạn.");
        vm.Notes.Add("Công việc tính theo những việc bạn tạo hoặc được giao. Dự án tính theo dự án bạn tạo hoặc là thành viên.");
        return vm;
    }

    // ================================================================== D. TÀI CHÍNH

    private async Task<ReportBaseViewModel> FinanceAsync(int uid, ReportRange r)
    {
        var cats = await _context.Categories.AsNoTracking().Where(c => c.UserId == uid || c.UserId == null).ToDictionaryAsync(c => c.Id, c => c);
        var tx = await _context.Transactions.AsNoTracking().Where(t => t.UserId == uid && t.TransactionDate >= r.From && t.TransactionDate <= r.To).ToListAsync();
        var prev = await _context.Transactions.AsNoTracking().Where(t => t.UserId == uid && t.TransactionDate >= r.PrevFrom && t.TransactionDate <= r.PrevTo).ToListAsync();
        // Chuyển tiền giữa các ví KHÔNG tính là thu nhập hay chi tiêu
        bool IsIn(LifeHub.Models.FinanceManagement.Transaction t) => t.TransactionType == "Income";
        bool IsOut(LifeHub.Models.FinanceManagement.Transaction t) => t.TransactionType == "Expense";
        var real = tx.Where(t => IsIn(t) || IsOut(t)).ToList();
        decimal income = real.Where(IsIn).Sum(t => t.Amount), expense = real.Where(IsOut).Sum(t => t.Amount);
        decimal pIncome = prev.Where(IsIn).Sum(t => t.Amount), pExpense = prev.Where(IsOut).Sum(t => t.Amount);
        var vm = Base("finance", "Finance", "Báo cáo tài chính", "Dòng tiền, chi tiêu theo danh mục, ngân sách và tiết kiệm.", CFinance);

        // Ngân sách: tháng của ngày cuối kỳ
        var m = r.To.Month; var y = r.To.Year;
        var budgets = await _context.Budgets.AsNoTracking().Where(b => b.UserId == uid && b.Month == m && b.Year == y).ToListAsync();
        var mStart = new DateOnly(y, m, 1); var mEnd = mStart.AddMonths(1).AddDays(-1);
        var bCats = budgets.Select(b => b.CategoryId).ToList();
        var monthSpend = await _context.Transactions.AsNoTracking().Where(t => t.UserId == uid && t.TransactionType == "Expense" && t.CategoryId != null && bCats.Contains(t.CategoryId.Value) && t.TransactionDate >= mStart && t.TransactionDate <= mEnd).ToListAsync();
        var limit = budgets.Sum(b => b.LimitAmount); var spent = monthSpend.Sum(t => t.Amount);

        var kIn = new ReportKpi { Badge = "INCOME", Title = "Tổng thu", Value = real.Any(IsIn) ? Money(income) : "--", Note = r.NoteText, Tooltip = "Tổng các giao dịch loại Thu nhập trong kỳ. Không gồm chuyển tiền giữa các ví." };
        var kOut = new ReportKpi { Badge = "EXPENSE", Title = "Tổng chi", Value = real.Any(IsOut) ? Money(expense) : "--", Note = r.NoteText, Tooltip = "Tổng các giao dịch loại Chi tiêu trong kỳ. Không gồm chuyển tiền giữa các ví." };
        if (real.Any(IsIn)) Delta(kIn, (double)income, (double)pIncome, r);
        if (real.Any(IsOut)) Delta(kOut, (double)expense, (double)pExpense, r);
        vm.Kpis.Add(kIn); vm.Kpis.Add(kOut);
        vm.Kpis.Add(new ReportKpi { Badge = "NET", Title = "Chênh lệch thu – chi", Value = real.Count > 0 ? (income - expense >= 0 ? "+" : "-") + Money(Math.Abs(income - expense)) : "--", Note = r.NoteText, Tooltip = "Tổng thu trừ tổng chi trong kỳ." });
        vm.Kpis.Add(new ReportKpi { Badge = "TRANSACTIONS", Title = "Số giao dịch", Value = real.Count > 0 ? real.Count.ToString() : "--", Unit = real.Count > 0 ? "giao dịch" : "", Note = r.NoteText, Tooltip = "Số giao dịch thu và chi (không đếm chuyển ví)." });
        vm.Kpis.Add(new ReportKpi { Badge = "BUDGET", Title = "Ngân sách đã sử dụng", Value = limit > 0 ? ReportMath.Pct((double)spent, (double)limit).ToString() : "--", Unit = limit > 0 ? "%" : "", Note = limit > 0 ? $"Tháng {m}/{y}: {Money(spent)} / {Money(limit)}" : "Chưa đặt ngân sách cho tháng này", Tooltip = "Tổng chi của các danh mục có ngân sách / tổng hạn mức, trong tháng chứa ngày cuối của kỳ." });

        var buckets = r.Buckets();
        List<double?> Sum(Func<LifeHub.Models.FinanceManagement.Transaction, bool> f) => buckets.Select(b => (double?)real.Where(f).Where(t => t.TransactionDate >= b.Start && t.TransactionDate <= b.End).Sum(t => (double)t.Amount)).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "cashflow", Title = "Dòng tiền", Subtitle = $"Thu và chi theo {r.BucketUnit}", Type = "bar", Wide = true,
            Data = new ChartData { Title = "Dòng tiền", Kind = "money", Labels = buckets.Select(b => b.Label).ToList(), Series = { new ChartSeries { Name = "Thu", Color = CProject, Values = Sum(IsIn) }, new ChartSeries { Name = "Chi", Color = CFinance, Values = Sum(IsOut) } } } });

        // Chi tiêu theo danh mục lấy từ bảng Category của người dùng, không hardcode
        var palette = new[] { CFinance, CTask, CGoal, CStudy, CLife, CHabit, "#94a3b8" };
        var byCat = real.Where(IsOut).GroupBy(t => t.CategoryId).Select(g => new { Id = g.Key, Sum = g.Sum(t => t.Amount) }).OrderByDescending(x => x.Sum).ToList();
        var slices = new List<DonutSlice>(); var i = 0;
        foreach (var c in byCat.Take(6))
        { var name = c.Id.HasValue && cats.TryGetValue(c.Id.Value, out var cat) ? cat.Name : "Chưa phân loại"; slices.Add(new DonutSlice { Label = name, Color = palette[i++ % palette.Length], Value = (double)c.Sum, ValueText = Money(c.Sum) }); }
        var rest = byCat.Skip(6).Sum(c => c.Sum);
        if (rest > 0) slices.Add(new DonutSlice { Label = "Khác", Color = "#94a3b8", Value = (double)rest, ValueText = Money(rest) });
        vm.Charts.Add(new ReportChartCard { Id = "cats", Title = "Chi tiêu theo danh mục", Subtitle = "Theo danh mục bạn đã tạo", Type = "donut", Slices = slices, CenterTop = expense > 0 ? Money(expense) : "", CenterBottom = "đã chi", Empty = "Chưa có khoản chi nào trong kỳ." });

        vm.Tables.Add(new ReportTable { Title = "Ngân sách tháng " + m + "/" + y, Subtitle = "Hạn mức và mức đã sử dụng theo danh mục", Headers = { "DANH MỤC", "HẠN MỨC", "ĐÃ CHI", "CÒN LẠI", "ĐÃ DÙNG" }, RightColumns = { 1, 2, 3, 4 }, Empty = "Chưa đặt ngân sách cho tháng này.",
            Rows = budgets.Select(b => { var s = monthSpend.Where(t => t.CategoryId == b.CategoryId).Sum(t => t.Amount); var pct = ReportMath.Pct((double)s, (double)b.LimitAmount);
                return new ReportRow { Cells = { ReportCell.T(cats.TryGetValue(b.CategoryId, out var c) ? c.Name : "Danh mục"), ReportCell.T(Money(b.LimitAmount)), ReportCell.T(Money(s)), ReportCell.T(Money(b.LimitAmount - s)), new ReportCell { Text = pct + "%", Status = pct >= 100 ? "st-overdue" : pct >= (b.AlertThresholdPercent ?? 80) ? "st-upcoming" : "st-graded" } } }; }).ToList() });

        var goals = await _context.SavingsGoals.AsNoTracking().Where(g => g.UserId == uid).OrderBy(g => g.Title).ToListAsync();
        vm.Tables.Add(new ReportTable { Title = "Mục tiêu tiết kiệm", Headers = { "MỤC TIÊU", "HIỆN CÓ", "MỤC TIÊU", "TIẾN ĐỘ", "HẠN" }, RightColumns = { 1, 2, 3 }, Empty = "Chưa có mục tiêu tiết kiệm nào.",
            Rows = goals.Select(g => new ReportRow { Cells = { new ReportCell { Text = g.Title, Color = g.Color ?? CFinance }, ReportCell.T(Money(g.CurrentAmount ?? 0)), ReportCell.T(Money(g.TargetAmount)), new ReportCell { Text = ReportMath.Pct((double)(g.CurrentAmount ?? 0), (double)g.TargetAmount) + "%", Status = g.IsAchieved ? "st-graded" : "st-submitted" }, ReportCell.T(g.Deadline?.ToString("dd/MM/yyyy", Inv) ?? "--") } }).ToList() });

        var gName = goals.ToDictionary(g => g.Id, g => g.Title);
        vm.Tables.Add(new ReportTable { Title = "Lịch sử đóng góp tiết kiệm", Subtitle = "Giao dịch gắn với mục tiêu tiết kiệm trong kỳ", Headers = { "NGÀY", "MỤC TIÊU", "SỐ TIỀN", "GHI CHÚ" }, RightColumns = { 2 }, PreviewRows = 6, Empty = "Chưa có đóng góp nào trong kỳ.",
            Rows = tx.Where(t => t.SavingsGoalId.HasValue).OrderByDescending(t => t.TransactionDate).Select(t => new ReportRow { Cells = { ReportCell.T(t.TransactionDate.ToString("dd/MM/yyyy", Inv)), ReportCell.T(gName.GetValueOrDefault(t.SavingsGoalId!.Value, "Mục tiêu")), ReportCell.T(Money(t.Amount)), ReportCell.T(t.Note ?? "") } }).ToList() });

        if (r.Compare && pExpense > 0 && expense > 0) { var d = ReportMath.Pct((double)(expense - pExpense), (double)pExpense); vm.Insights.Add(d > 0 ? $"Chi tiêu trong kỳ tăng {d}% so với kỳ trước." : d < 0 ? $"Chi tiêu trong kỳ giảm {-d}% so với kỳ trước." : "Chi tiêu giữ nguyên so với kỳ trước."); }
        if (byCat.Count > 0 && expense > 0) { var top = byCat[0]; vm.Insights.Add($"Danh mục chi nhiều nhất là {(top.Id.HasValue && cats.TryGetValue(top.Id.Value, out var tc) ? tc.Name : "Chưa phân loại")} ({ReportMath.Pct((double)top.Sum, (double)expense)}% tổng chi)."); }
        var over = budgets.Count(b => monthSpend.Where(t => t.CategoryId == b.CategoryId).Sum(t => t.Amount) >= b.LimitAmount);
        if (over > 0) vm.Insights.Add($"{over} danh mục đã dùng hết hạn mức ngân sách tháng {m}/{y}.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét.");
        vm.Notes.Add("Giao dịch chuyển tiền giữa các ví không được tính là thu nhập hay chi tiêu, để tránh tính trùng dòng tiền.");
        vm.Notes.Add("Số tiền hiển thị theo đúng đơn vị đã nhập, không quy đổi ngoại tệ.");
        return vm;
    }

    // ================================================================== E. MỤC TIÊU & THÓI QUEN

    private async Task<ReportBaseViewModel> GoalsHabitsAsync(int uid, ReportRange r, string? goalStatus, string? goalType)
    {
        var allGoals = await _context.Goals.AsNoTracking().Where(g => g.UserId == uid).OrderBy(g => g.TargetDate == null).ThenBy(g => g.TargetDate).ToListAsync();
        var gIds = allGoals.Select(g => g.Id).ToList();
        var krs = await _context.KeyResults.AsNoTracking().Where(k => gIds.Contains(k.GoalId)).ToListAsync();
        var habits = await _context.Habits.AsNoTracking().Where(h => h.UserId == uid && h.Active).OrderBy(h => h.Title).ToListAsync();
        var hIds = habits.Select(h => h.Id).ToList();
        var logs = await _context.HabitLogs.AsNoTracking().Where(l => hIds.Contains(l.HabitId) && l.CheckDate >= r.PrevFrom && l.CheckDate <= r.To).ToListAsync();
        var byHabit = logs.ToLookup(l => l.HabitId);
        var vm = Base("goals", "Productivity", "Báo cáo mục tiêu & thói quen", "Tiến độ mục tiêu và mức độ duy trì thói quen của bạn.", CGoal);
        goalStatus = goalStatus is "active" or "completed" ? goalStatus : "all";
        goalType = string.IsNullOrWhiteSpace(goalType) ? "all" : goalType;
        vm.GoalStatus = goalStatus; vm.GoalType = goalType;
        vm.GoalTypes = allGoals.Select(g => g.GoalType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();

        bool Completed(LifeHub.Models.GoalManagement.Goal g) => g.IsCompleted || IsOneOf(g.Status, "completed");
        var live = allGoals.Where(g => !IsOneOf(g.Status, "cancelled", "archived")).ToList();
        int hExp = 0, hDone = 0, pExp = 0, pDone = 0;
        var rows = new List<(Models.HabitManagement.Habit H, int Exp, int Done, int Days)>();
        foreach (var h in habits)
        {
            var c = ReportMath.HabitPeriod(h, byHabit[h.Id], r.From, r.To, r.Today); hExp += c.Expected; hDone += c.Done;
            var p = ReportMath.HabitPeriod(h, byHabit[h.Id], r.PrevFrom, r.PrevTo, r.Today); pExp += p.Expected; pDone += p.Done;
            rows.Add((h, c.Expected, c.Done, byHabit[h.Id].Count(l => l.IsCompleted && l.CheckDate >= r.From)));
        }
        var best = habits.OrderByDescending(h => h.CurrentStreak).FirstOrDefault(h => h.CurrentStreak > 0);

        vm.Kpis.Add(new ReportKpi { Badge = "GOALS", Title = "Tổng mục tiêu", Value = allGoals.Count > 0 ? allGoals.Count.ToString() : "--", Unit = allGoals.Count > 0 ? "mục tiêu" : "", Note = "Tại thời điểm báo cáo", Tooltip = "Tất cả mục tiêu của bạn." });
        vm.Kpis.Add(new ReportKpi { Badge = "COMPLETED", Title = "Mục tiêu hoàn thành", Value = allGoals.Count > 0 ? allGoals.Count(Completed).ToString() : "--", Unit = allGoals.Count > 0 ? "mục tiêu" : "", Note = "Tại thời điểm báo cáo", Tooltip = "Mục tiêu đã đánh dấu hoàn thành hoặc có trạng thái completed." });
        vm.Kpis.Add(new ReportKpi { Badge = "PROGRESS", Title = "Tiến độ mục tiêu trung bình", Value = live.Count > 0 ? Math.Round(live.Average(g => g.ProgressPercent)).ToString("0") : "--", Unit = live.Count > 0 ? "%" : "", Note = "Trung bình ProgressPercent", Tooltip = "Trung bình ProgressPercent của các mục tiêu chưa hủy, chưa lưu trữ." });
        var kh = new ReportKpi { Badge = "HABITS", Title = "Tỷ lệ duy trì thói quen", Value = hExp > 0 ? ReportMath.Pct(hDone, hExp).ToString() : "--", Unit = hExp > 0 ? "%" : "", Note = $"{hDone}/{hExp} lần, {r.NoteText}", Tooltip = "Số lần hoàn thành / số lần dự kiến theo lịch của từng thói quen đang hoạt động, chỉ tính từ ngày thói quen bắt đầu." };
        if (hExp > 0 && pExp > 0) Delta(kh, ReportMath.Pct(hDone, hExp), ReportMath.Pct(pDone, pExp), r, points: true);
        vm.Kpis.Add(kh);
        vm.Kpis.Add(new ReportKpi { Badge = "STREAK", Title = "Streak tốt nhất", Value = best == null ? "--" : best.CurrentStreak.ToString(), Unit = best == null ? "" : (best.Frequency.StartsWith("week", StringComparison.OrdinalIgnoreCase) ? "tuần" : "ngày"), Note = best?.Title ?? "Chưa có chuỗi nào", Tooltip = "Streak hiện tại cao nhất trong các thói quen đang hoạt động (giá trị lưu trong thói quen)." });

        // Biểu đồ: tỷ lệ duy trì theo từng cột thời gian
        var buckets = r.Buckets();
        var rateByBucket = buckets.Select(b => { int e = 0, d = 0; foreach (var h in habits) { var c = ReportMath.HabitPeriod(h, byHabit[h.Id], b.Start, b.End, r.Today); e += c.Expected; d += c.Done; } return e == 0 ? (double?)null : ReportMath.Pct(d, e); }).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "habitrate", Title = "Tỷ lệ duy trì thói quen", Subtitle = $"Theo {r.BucketUnit}. Khoảng trống là chưa có dữ liệu, không phải 0%", Type = "bar", Wide = true, Empty = "Chưa có thói quen nào đang hoạt động trong kỳ.",
            Data = new ChartData { Title = "Tỷ lệ duy trì thói quen", Kind = "pct", Labels = buckets.Select(b => b.Label).ToList(), Series = { new ChartSeries { Name = "Tỷ lệ duy trì", Color = CHabit, Values = rateByBucket } } } });

        // Bảng mục tiêu (có lọc trạng thái và loại)
        var shown = allGoals.Where(g => goalStatus == "all" || (goalStatus == "completed" ? Completed(g) : !Completed(g))).Where(g => goalType == "all" || g.GoalType == goalType).ToList();
        (int Done, int Total) Kr(int goalId) { var k = krs.Where(x => x.GoalId == goalId).ToList(); return (k.Count(x => x.IsCompleted), k.Count); }
        vm.Tables.Add(new ReportTable { Title = "Báo cáo mục tiêu", Subtitle = $"{shown.Count}/{allGoals.Count} mục tiêu theo bộ lọc", Headers = { "MỤC TIÊU", "TIẾN ĐỘ", "THỜI HẠN", "TRẠNG THÁI", "KEY RESULTS" }, RightColumns = { 1, 4 }, PreviewRows = 8, Empty = allGoals.Count == 0 ? "Chưa có mục tiêu nào. Hãy tạo mục tiêu ở phân hệ Mục tiêu." : "Không có mục tiêu nào phù hợp bộ lọc.",
            Rows = shown.Select(g => { var k = Kr(g.Id);
                return new ReportRow { Cells = { ReportCell.T(g.Title, string.Join(", ", new[] { g.Category, g.GoalType }.Where(s => !string.IsNullOrWhiteSpace(s)))), ReportCell.T(g.ProgressPercent + "%"), ReportCell.T(g.TargetDate?.ToString("dd/MM/yyyy", Inv) ?? "--"),
                    Completed(g) ? new ReportCell { Text = "Hoàn thành", Status = "st-graded" } : IsOneOf(g.Status, "cancelled", "archived") ? new ReportCell { Text = "Đã đóng", Status = "st-rest" } : g.TargetDate.HasValue && g.TargetDate < r.Today ? new ReportCell { Text = "Quá hạn", Status = "st-overdue" } : new ReportCell { Text = "Đang thực hiện", Status = "st-submitted" },
                    ReportCell.T(k.Total == 0 ? "--" : $"{k.Done}/{k.Total}") } }; }).ToList() });

        vm.Tables.Add(new ReportTable { Title = "Báo cáo thói quen", Subtitle = $"Các thói quen đang hoạt động, {r.NoteText}", Headers = { "THÓI QUEN", "TỶ LỆ", "ĐÃ LÀM / DỰ KIẾN", "NGÀY THỰC HIỆN", "CHƯA ĐẠT", "STREAK HIỆN TẠI", "STREAK DÀI NHẤT" }, RightColumns = { 1, 2, 3, 4, 5, 6 }, Empty = "Chưa có thói quen nào đang hoạt động.",
            Rows = rows.Select(x => { var unit = x.H.Frequency.StartsWith("week", StringComparison.OrdinalIgnoreCase) ? "tuần" : "ngày";
                return new ReportRow { Cells = { new ReportCell { Text = x.H.Title, Color = x.H.ColorCode ?? CHabit, Sub = unit == "tuần" ? $"{x.H.TargetPerPeriod} ngày mỗi tuần" : $"{x.H.TargetPerPeriod} lần mỗi ngày" }, ReportCell.T(x.Exp > 0 ? ReportMath.Pct(x.Done, x.Exp) + "%" : "--"), ReportCell.T($"{x.Done}/{x.Exp}"), ReportCell.T(x.Days.ToString()), ReportCell.T(x.Exp > 0 ? (x.Exp - x.Done).ToString() : "--"), ReportCell.T($"{x.H.CurrentStreak} {unit}"), ReportCell.T($"{x.H.LongestStreak} {unit}") } }; }).ToList() });

        // Kết nối Goal – Habit: chỉ thể hiện liên kết dữ liệu do người dùng thiết lập
        var linked = allGoals.Where(g => habits.Any(h => h.GoalId == g.Id)).ToList();
        vm.Tables.Add(new ReportTable { Title = "Kết nối mục tiêu và thói quen", Subtitle = "Các thói quen bạn đã liên kết với từng mục tiêu", Headers = { "MỤC TIÊU", "TIẾN ĐỘ", "KEY RESULTS", "THÓI QUEN LIÊN KẾT", "TỶ LỆ DUY TRÌ" }, RightColumns = { 1, 2, 4 }, Empty = "Chưa có thói quen nào được liên kết với mục tiêu. Bạn có thể chọn mục tiêu khi tạo thói quen.",
            Rows = linked.Select(g => { var hs = rows.Where(x => x.H.GoalId == g.Id).ToList(); var e = hs.Sum(x => x.Exp); var d = hs.Sum(x => x.Done); var k = Kr(g.Id);
                return new ReportRow { Cells = { ReportCell.T(g.Title), ReportCell.T(g.ProgressPercent + "%"), ReportCell.T(k.Total == 0 ? "--" : $"{k.Done}/{k.Total}"), ReportCell.T(string.Join(", ", hs.Select(x => x.H.Title))), ReportCell.T(e > 0 ? ReportMath.Pct(d, e) + "%" : "--") } }; }).ToList() });

        var ranked = rows.Where(x => x.Exp >= 3).OrderByDescending(x => ReportMath.Pct(x.Done, x.Exp)).ToList();
        if (ranked.Count >= 2 && ReportMath.Pct(ranked[0].Done, ranked[0].Exp) - ReportMath.Pct(ranked[^1].Done, ranked[^1].Exp) >= 15)
        { vm.Insights.Add($"Thói quen \"{ranked[0].H.Title}\" được duy trì tốt nhất ({ReportMath.Pct(ranked[0].Done, ranked[0].Exp)}%)."); vm.Insights.Add($"Thói quen \"{ranked[^1].H.Title}\" đang cần được quan tâm hơn ({ReportMath.Pct(ranked[^1].Done, ranked[^1].Exp)}%)."); }
        var near = live.Count(g => !Completed(g) && g.TargetDate.HasValue && g.TargetDate >= r.Today && g.TargetDate <= r.Today.AddDays(14) && g.ProgressPercent < 50);
        if (near > 0) vm.Insights.Add($"{near} mục tiêu sắp đến hạn trong 14 ngày tới nhưng tiến độ còn dưới 50%.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét.");
        vm.Notes.Add("Mục tiêu và thói quen chỉ được kết nối bằng liên kết do bạn thiết lập. Báo cáo không khẳng định thói quen là nguyên nhân khiến mục tiêu hoàn thành.");
        vm.Notes.Add("Bộ lọc thời gian áp dụng cho thói quen. Bảng mục tiêu hiển thị tình trạng tại thời điểm báo cáo và lọc theo trạng thái, loại.");
        return vm;
    }

    // ================================================================== F. SINH HOẠT

    private async Task<ReportBaseViewModel> LifestyleAsync(int uid, ReportRange r)
    {
        var cur = await _context.LifestyleLogs.AsNoTracking().Where(l => l.UserId == uid && l.LogDate >= r.From && l.LogDate <= r.To).ToListAsync();
        var prv = await _context.LifestyleLogs.AsNoTracking().Where(l => l.UserId == uid && l.LogDate >= r.PrevFrom && l.LogDate <= r.PrevTo).ToListAsync();
        var vm = Base("lifestyle", "Lifestyle", "Báo cáo sinh hoạt", "Tổng hợp thói quen sinh hoạt từ dữ liệu bạn đã ghi nhận.", CLife);

        double? AvgSleep(List<LifeHub.Models.LifestyleManagement.LifestyleLog> l) { var v = l.Where(x => x.SleepHours.HasValue).ToList(); return v.Count == 0 ? null : v.Average(x => (double)x.SleepHours!.Value); }
        double? AvgWater(List<LifeHub.Models.LifestyleManagement.LifestyleLog> l) { var v = l.Where(x => x.WaterMl.HasValue).ToList(); return v.Count == 0 ? null : v.Average(x => (double)x.WaterMl!.Value); }
        int Act(List<LifeHub.Models.LifestyleManagement.LifestyleLog> l) => l.Sum(x => x.PhysicalActivityMinutes ?? 0);
        double? AvgAct(List<LifeHub.Models.LifestyleManagement.LifestyleLog> l) { var v = l.Where(x => x.PhysicalActivityMinutes.HasValue).ToList(); return v.Count == 0 ? null : v.Average(x => (double)x.PhysicalActivityMinutes!.Value); }
        int Days(List<LifeHub.Models.LifestyleManagement.LifestyleLog> l) => l.Select(x => x.LogDate).Distinct().Count();

        var s = AvgSleep(cur); var w = AvgWater(cur); var a = AvgAct(cur);
        var waterTotal = cur.Where(x => x.WaterMl.HasValue).Sum(x => x.WaterMl!.Value);
        vm.Kpis.Add(new ReportKpi { Badge = "SLEEP", Title = "Thời lượng ngủ trung bình", Value = s.HasValue ? s.Value.ToString("0.0", Inv) : "--", Unit = s.HasValue ? "giờ / đêm" : "", Note = r.NoteText, Tooltip = "Trung bình SleepHours của những ngày có ghi nhận giấc ngủ." });
        vm.Kpis.Add(new ReportKpi { Badge = "WATER", Title = "Lượng nước ghi nhận", Value = w.HasValue ? (waterTotal / 1000.0).ToString("0.0", Inv) : "--", Unit = w.HasValue ? "lít" : "", Note = w.HasValue ? $"Trung bình {Math.Round(w.Value)} ml / ngày có ghi nhận" : r.NoteText, Tooltip = "Tổng WaterMl đã ghi nhận trong kỳ." });
        vm.Kpis.Add(new ReportKpi { Badge = "ACTIVITY", Title = "Thời gian vận động", Value = cur.Any(x => x.PhysicalActivityMinutes.HasValue) ? Hours(Act(cur)) : "--", Unit = cur.Any(x => x.PhysicalActivityMinutes.HasValue) ? "giờ" : "", Note = a.HasValue ? $"Trung bình {Math.Round(a.Value)} phút / ngày có ghi nhận" : r.NoteText, Tooltip = "Tổng PhysicalActivityMinutes trong kỳ." });
        vm.Kpis.Add(new ReportKpi { Badge = "DAYS", Title = "Số ngày có ghi nhận", Value = cur.Count > 0 ? Days(cur).ToString() : "--", Unit = cur.Count > 0 ? $"/ {r.Days} ngày" : "", Note = r.NoteText, Tooltip = "Số ngày khác nhau có ít nhất một bản ghi sinh hoạt." });

        var buckets = r.Buckets();
        List<double?> PerBucket(Func<List<LifeHub.Models.LifestyleManagement.LifestyleLog>, double?> f) => buckets.Select(b => f(cur.Where(x => x.LogDate >= b.Start && x.LogDate <= b.End).ToList())).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "sleep", Title = "Xu hướng giấc ngủ", Subtitle = $"Giờ ngủ trung bình theo {r.BucketUnit}. Khoảng trống là chưa có ghi nhận", Type = "line", Empty = "Chưa có ghi nhận giấc ngủ trong kỳ.",
            Data = new ChartData { Title = "Xu hướng giấc ngủ", Kind = "dec1", Labels = buckets.Select(b => b.Label).ToList(), Series = { new ChartSeries { Name = "Giờ ngủ", Color = CLife, Values = PerBucket(AvgSleep) } } } });
        vm.Charts.Add(new ReportChartCard { Id = "water", Title = "Lượng nước theo thời gian", Subtitle = $"ml / ngày có ghi nhận, theo {r.BucketUnit}", Type = "bar", Empty = "Chưa có ghi nhận lượng nước trong kỳ.",
            Data = new ChartData { Title = "Lượng nước", Kind = "int", Labels = buckets.Select(b => b.Label).ToList(), Series = { new ChartSeries { Name = "Nước (ml)", Color = CStudy, Values = PerBucket(AvgWater) } } } });
        vm.Charts.Add(new ReportChartCard { Id = "activity", Title = "Thời gian vận động", Subtitle = $"Tổng số phút theo {r.BucketUnit}", Type = "bar", Empty = "Chưa có ghi nhận vận động trong kỳ.",
            Data = new ChartData { Title = "Thời gian vận động", Kind = "int", Labels = buckets.Select(b => b.Label).ToList(), Series = { new ChartSeries { Name = "Phút", Color = CTask, Values = PerBucket(l => l.Any(x => x.PhysicalActivityMinutes.HasValue) ? Act(l) : (double?)null) } } } });
        var shades = new[] { "#a7f3d0", "#6ee7b7", "#34d399", "#10b981", "#047857" };
        var moods = Enumerable.Range(1, 5).Select(m => new DonutSlice { Label = "Mức " + m, Color = shades[m - 1], Value = cur.Count(x => x.MoodRate == m), ValueText = cur.Count(x => x.MoodRate == m) + " ngày" }).ToList();
        vm.Charts.Add(new ReportChartCard { Id = "mood", Title = "Phân bố tâm trạng", Subtitle = "Số ngày theo mức tâm trạng bạn tự chấm (1 thấp, 5 cao)", Type = "donut", Slices = moods, CenterTop = moods.Sum(x => x.Value).ToString("0"), CenterBottom = "ngày", Empty = "Chưa có ghi nhận tâm trạng trong kỳ." });

        if (r.Compare)
        {
            string Cmp(double? c, double? p, string fmt, string unit) => !c.HasValue || !p.HasValue ? "--" : (c.Value - p.Value >= 0 ? "+" : "-") + Math.Abs(c.Value - p.Value).ToString(fmt, Inv) + " " + unit;
            string V(double? v, string fmt, string unit) => v.HasValue ? v.Value.ToString(fmt, Inv) + " " + unit : "--";
            vm.Tables.Add(new ReportTable { Title = "So sánh với kỳ trước", Subtitle = $"{r.Text} so với {r.PrevText}", Headers = { "CHỈ SỐ", "KỲ HIỆN TẠI", "KỲ TRƯỚC", "BIẾN ĐỘNG" }, RightColumns = { 1, 2, 3 },
                Rows = {
                    new() { Cells = { ReportCell.T("Giờ ngủ trung bình"), ReportCell.T(V(s, "0.0", "giờ")), ReportCell.T(V(AvgSleep(prv), "0.0", "giờ")), ReportCell.T(Cmp(s, AvgSleep(prv), "0.0", "giờ")) } },
                    new() { Cells = { ReportCell.T("Nước trung bình mỗi ngày"), ReportCell.T(V(w, "0", "ml")), ReportCell.T(V(AvgWater(prv), "0", "ml")), ReportCell.T(Cmp(w, AvgWater(prv), "0", "ml")) } },
                    new() { Cells = { ReportCell.T("Vận động trung bình mỗi ngày"), ReportCell.T(V(a, "0", "phút")), ReportCell.T(V(AvgAct(prv), "0", "phút")), ReportCell.T(Cmp(a, AvgAct(prv), "0", "phút")) } },
                    new() { Cells = { ReportCell.T("Số ngày có ghi nhận"), ReportCell.T(Days(cur).ToString()), ReportCell.T(Days(prv).ToString()), ReportCell.T((Days(cur) - Days(prv) >= 0 ? "+" : "") + (Days(cur) - Days(prv))) } } } });
        }

        if (r.Compare && s.HasValue && AvgSleep(prv) is double ps && Math.Abs(s.Value - ps) >= 0.3) vm.Insights.Add($"Giờ ngủ trung bình {(s.Value > ps ? "tăng" : "giảm")} {Math.Abs(s.Value - ps).ToString("0.0", Inv)} giờ so với kỳ trước.");
        if (cur.Count > 0) vm.Insights.Add($"Bạn có ghi nhận sinh hoạt trong {Days(cur)}/{r.Days} ngày của kỳ.");
        if (vm.Insights.Count == 0) vm.Insights.Add("Chưa đủ dữ liệu để đưa ra nhận xét. Hãy ghi nhận sinh hoạt thêm vài ngày.");
        vm.Notes.Add("Báo cáo chỉ tổng hợp những gì bạn đã ghi nhận, không tạo chỉ số sức khỏe tổng hợp, không chẩn đoán và không đánh giá cơ thể.");
        vm.Notes.Add("Mục tiêu sinh hoạt cá nhân chưa có trong dữ liệu nên chưa so sánh thực tế với mục tiêu.");
        return vm;
    }

    // ================================================================== 7. POMODORO
    private async Task<ReportBaseViewModel> PomodoroAsync(int uid, ReportRange r)
    {
        var vm = Base("pomodoro", "Pomodoro", "Thống kê Pomodoro", "Tổng hợp thời gian tập trung, số phiên hoàn thành và phân bổ theo công việc, môn học.", CTime);

        var cur = await _context.PomodoroSessions.AsNoTracking()
            .Where(p => p.UserId == uid && p.IsCompleted && p.StartedAt >= r.FromDt && p.StartedAt < r.ToExclusive)
            .OrderByDescending(p => p.StartedAt).ToListAsync();

        var prv = r.Compare
            ? await _context.PomodoroSessions.AsNoTracking()
                .Where(p => p.UserId == uid && p.IsCompleted && p.StartedAt >= r.PrevFromDt && p.StartedAt < r.PrevToExclusive)
                .ToListAsync()
            : new List<LifeHub.Models.TimeManagement.PomodoroSession>();

        var curMins = cur.Sum(p => p.DurationMinutes);
        var prvMins = prv.Sum(p => p.DurationMinutes);
        var avgMins = cur.Count > 0 ? (int)Math.Round(curMins / (double)cur.Count) : 0;
        var activeDays = cur.Select(p => DateOnly.FromDateTime(p.StartedAt)).Distinct().Count();

        var kTime = new ReportKpi { Badge = "FOCUS", Title = "Tổng thời gian tập trung", Value = cur.Count > 0 ? Hours(curMins) : "--", Unit = cur.Count > 0 ? "giờ" : "", Note = cur.Count > 0 ? $"{curMins} phút trong kỳ" : r.NoteText, Tooltip = "Tổng DurationMinutes của các phiên Pomodoro đã hoàn thành trong kỳ." };
        Delta(kTime, curMins, prvMins, r);
        vm.Kpis.Add(kTime);

        var kSess = new ReportKpi { Badge = "SESSIONS", Title = "Số phiên hoàn thành", Value = cur.Count > 0 ? cur.Count.ToString() : "--", Unit = cur.Count > 0 ? "phiên" : "", Note = r.NoteText, Tooltip = "Tổng số phiên Pomodoro có IsCompleted = true trong kỳ." };
        Delta(kSess, cur.Count, prv.Count, r);
        vm.Kpis.Add(kSess);

        vm.Kpis.Add(new ReportKpi { Badge = "AVG", Title = "Trung bình mỗi phiên", Value = cur.Count > 0 ? avgMins.ToString() : "--", Unit = cur.Count > 0 ? "phút" : "", Note = cur.Count > 0 ? "Thời lượng tập trung bình quân" : r.NoteText });
        vm.Kpis.Add(new ReportKpi { Badge = "DAYS", Title = "Số ngày có tập trung", Value = cur.Count > 0 ? activeDays.ToString() : "--", Unit = cur.Count > 0 ? $"/ {r.Days} ngày" : "", Note = r.NoteText });

        var buckets = r.Buckets();
        vm.Charts.Add(new ReportChartCard
        {
            Id = "pomo-mins",
            Title = "Thời gian tập trung theo thời gian",
            Subtitle = $"Tổng số phút Pomodoro theo {r.BucketUnit}",
            Type = "bar",
            Empty = "Chưa có phiên Pomodoro nào trong kỳ.",
            Data = new ChartData
            {
                Title = "Phút tập trung",
                Kind = "int",
                Labels = buckets.Select(b => b.Label).ToList(),
                Series = { new ChartSeries { Name = "Phút tập trung", Color = CTime, Values = buckets.Select(b => (double?)cur.Where(x => DateOnly.FromDateTime(x.StartedAt) >= b.Start && DateOnly.FromDateTime(x.StartedAt) <= b.End).Sum(x => x.DurationMinutes)).ToList() } }
            }
        });

        vm.Charts.Add(new ReportChartCard
        {
            Id = "pomo-count",
            Title = "Số phiên hoàn thành",
            Subtitle = $"Số phiên theo {r.BucketUnit}",
            Type = "line",
            Empty = "Chưa có phiên Pomodoro nào trong kỳ.",
            Data = new ChartData
            {
                Title = "Số phiên",
                Kind = "int",
                Labels = buckets.Select(b => b.Label).ToList(),
                Series = { new ChartSeries { Name = "Số phiên", Color = CStudy, Values = buckets.Select(b => (double?)cur.Count(x => DateOnly.FromDateTime(x.StartedAt) >= b.Start && DateOnly.FromDateTime(x.StartedAt) <= b.End)).ToList() } }
            }
        });

        var taskMins = cur.Where(p => p.TaskId.HasValue).Sum(p => p.DurationMinutes);
        var courseMins = cur.Where(p => !p.TaskId.HasValue && p.CourseId.HasValue).Sum(p => p.DurationMinutes);
        var freeMins = cur.Where(p => !p.TaskId.HasValue && !p.CourseId.HasValue).Sum(p => p.DurationMinutes);
        var slices = new List<DonutSlice>
        {
            new() { Label = "Công việc", Color = CTask, Value = taskMins, ValueText = $"{taskMins} phút" },
            new() { Label = "Môn học", Color = CStudy, Value = courseMins, ValueText = $"{courseMins} phút" },
            new() { Label = "Tập trung tự do", Color = CTime, Value = freeMins, ValueText = $"{freeMins} phút" }
        };
        vm.Charts.Add(new ReportChartCard
        {
            Id = "pomo-dist",
            Title = "Phân bổ theo đối tượng",
            Subtitle = "Tỷ trọng thời gian tập trung giữa công việc, môn học và tự do",
            Type = "donut",
            Slices = slices,
            CenterTop = Hours(curMins),
            CenterBottom = "giờ",
            Empty = "Chưa có dữ liệu phân bổ trong kỳ."
        });

        var taskIds = cur.Where(p => p.TaskId.HasValue).Select(p => p.TaskId!.Value).Distinct().ToList();
        var courseIds = cur.Where(p => p.CourseId.HasValue).Select(p => p.CourseId!.Value).Distinct().ToList();
        var taskNames = taskIds.Any()
            ? await _context.Tasks.AsNoTracking().Where(t => taskIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Title)
            : new Dictionary<int, string>();
        var courseNames = courseIds.Any()
            ? await _context.Courses.AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CourseName)
            : new Dictionary<int, string>();

        string TargetLabel(LifeHub.Models.TimeManagement.PomodoroSession p) =>
            p.TaskId.HasValue ? (taskNames.TryGetValue(p.TaskId.Value, out var tn) ? tn : "Công việc")
            : p.CourseId.HasValue ? (courseNames.TryGetValue(p.CourseId.Value, out var cn) ? cn : "Môn học")
            : "Tập trung tự do";

        string TargetKind(LifeHub.Models.TimeManagement.PomodoroSession p) =>
            p.TaskId.HasValue ? "Công việc" : p.CourseId.HasValue ? "Môn học" : "Tự do";

        var tableRows = cur
            .GroupBy(p => new { Name = TargetLabel(p), Kind = TargetKind(p) })
            .Select(g => new { g.Key.Name, g.Key.Kind, Sessions = g.Count(), Minutes = g.Sum(x => x.DurationMinutes) })
            .OrderByDescending(x => x.Minutes)
            .Take(10)
            .Select(x =>
            {
                var pct = curMins > 0 ? Math.Round(x.Minutes * 100.0 / curMins) : 0;
                return new ReportRow
                {
                    Cells =
                    {
                        ReportCell.T(x.Name),
                        ReportCell.T(x.Kind),
                        ReportCell.T(x.Sessions.ToString()),
                        ReportCell.T($"{x.Minutes} phút"),
                        ReportCell.T($"{pct}%")
                    }
                };
            }).ToList();

        vm.Tables.Add(new ReportTable
        {
            Title = "Chi tiết theo mục tiêu tập trung",
            Subtitle = "Top 10 công việc / môn học có thời gian Pomodoro cao nhất trong kỳ",
            Headers = { "MỤC TIÊU", "PHÂN LOẠI", "SỐ PHIÊN", "TỔNG THỜI GIAN", "TỶ TRỌNG" },
            RightColumns = { 2, 3, 4 },
            Rows = tableRows,
            Empty = "Chưa có phiên Pomodoro nào được ghi nhận trong kỳ."
        });

        if (cur.Count > 0)
            vm.Insights.Add($"Bạn đã hoàn thành {cur.Count} phiên Pomodoro với tổng cộng {curMins} phút ({Hours(curMins)} giờ) trong {activeDays} ngày.");
        if (r.Compare && prv.Count > 0 && curMins != prvMins)
            vm.Insights.Add($"Tổng thời gian tập trung {(curMins > prvMins ? "tăng" : "giảm")} {Math.Abs(curMins - prvMins)} phút so với kỳ trước.");
        if (vm.Insights.Count == 0)
            vm.Insights.Add("Chưa có phiên Pomodoro nào trong kỳ. Mở mục Pomodoro trong Lịch và thời gian để bắt đầu.");

        vm.Notes.Add("Số liệu tính từ các phiên Pomodoro có trạng thái hoàn thành (IsCompleted = true) trong khoảng thời gian đã chọn.");
        return vm;
    }

    // ================================================================== Hàm hỗ trợ

    private ReportBaseViewModel Base(string section, string action, string title, string desc, string color) =>
        new() { Section = section, Action = action, Title = title, Description = desc, SubColor = color };

    private static bool IsOneOf(string? value, params string[] options) => options.Contains((value ?? "").Trim().ToLowerInvariant());
    private static string Money(decimal v) => v.ToString("N0", Vi) + " ₫";
    private static string Hours(int minutes) => (minutes / 60.0).ToString("0.#", Inv);

    /// <summary>Biến động so với kỳ trước. Chỉ hiện khi bật so sánh và kỳ trước có dữ liệu hợp lệ (> 0). points = true: tính theo điểm phần trăm.</summary>
    private static void Delta(ReportKpi k, double cur, double prev, ReportRange r, bool points = false)
    {
        if (!r.Compare || prev <= 0) return;
        if (points)
        {
            var d = (int)Math.Round(cur - prev);
            k.Delta = d == 0 ? "Không đổi so với kỳ trước" : $"{(d > 0 ? "+" : "-")}{Math.Abs(d)} điểm % so với kỳ trước"; k.DeltaDir = d > 0 ? "up" : d < 0 ? "down" : "flat";
        }
        else
        {
            var p = (int)Math.Round((cur - prev) * 100 / prev);
            k.Delta = p == 0 ? "Không đổi so với kỳ trước" : $"{(p > 0 ? "+" : "-")}{Math.Abs(p)}% so với kỳ trước"; k.DeltaDir = p > 0 ? "up" : p < 0 ? "down" : "flat";
        }
    }
}
