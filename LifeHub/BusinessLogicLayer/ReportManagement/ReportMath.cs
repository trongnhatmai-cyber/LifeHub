using LifeHub.Models.HabitManagement;

namespace LifeHub.BusinessLogicLayer.ReportManagement;

/// <summary>Công thức dùng chung cho báo cáo (độc lập với các phân hệ khác, chỉ dùng cột đã có).</summary>
public static class ReportMath
{
    public static int Pct(double part, double whole) => whole <= 0 ? 0 : (int)Math.Round(part * 100.0 / whole);

    /// <summary>
    /// (Số lần dự kiến, số lần hoàn thành) của một thói quen trong [from, to].
    /// Quy ước: Frequency "weekly" = TargetPerPeriod ngày mỗi tuần (tối đa 7), còn lại = TargetPerPeriod lần mỗi ngày.
    /// Chỉ tính từ ngày bắt đầu (ngày tạo hoặc ngày check-in sớm nhất) đến hôm nay; ngày trước đó là "chưa có dữ liệu".
    /// Thói quen tuần: dự kiến của tuần chưa đủ 7 ngày được tính theo tỷ lệ, làm tròn lên.
    /// </summary>
    public static (int Expected, int Done) HabitPeriod(Habit h, IEnumerable<HabitLog> logs, DateOnly from, DateOnly to, DateOnly today)
    {
        var f = (h.Frequency ?? "").Trim().ToLowerInvariant();
        var weekly = f.StartsWith("week") || f.Contains("tuần") || f.Contains("tuan");
        var target = Math.Max(1, h.TargetPerPeriod);
        if (weekly) target = Math.Min(target, 7);
        var goal = weekly ? 1 : target;

        var counts = new Dictionary<DateOnly, int>();
        foreach (var l in logs) counts[l.CheckDate] = l.IsCompleted ? Math.Max(l.CountValue, goal) : Math.Max(0, l.CountValue);

        DateOnly? created = h.CreatedAt.Year >= 2000 ? DateOnly.FromDateTime(h.CreatedAt) : null;
        DateOnly? first = counts.Count > 0 ? counts.Keys.Min() : null;
        var start = created.HasValue && first.HasValue ? (created < first ? created.Value : first.Value) : created ?? first ?? today;

        var a = from > start ? from : start;
        var b = to < today ? to : today;
        if (a > b) return (0, 0);
        bool Done(DateOnly d) => counts.TryGetValue(d, out var c) && c >= goal;

        int expected = 0, done = 0;
        if (!weekly)
        {
            for (var d = a; d <= b; d = d.AddDays(1)) { expected += goal; done += Math.Min(counts.GetValueOrDefault(d), goal); }
            return (expected, done);
        }
        var ws = a.AddDays(-(((int)a.DayOfWeek + 6) % 7));
        for (; ws <= b; ws = ws.AddDays(7))
        {
            var x = ws > a ? ws : a;
            var y = ws.AddDays(6) < b ? ws.AddDays(6) : b;
            var days = y.DayNumber - x.DayNumber + 1;
            var exp = Math.Min(target, (int)Math.Ceiling(target * days / 7.0));
            var doneDays = 0;
            for (var d = x; d <= y; d = d.AddDays(1)) if (Done(d)) doneDays++;
            expected += exp; done += Math.Min(doneDays, exp);
        }
        return (expected, done);
    }
}
