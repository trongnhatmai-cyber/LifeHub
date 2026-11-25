using System.Globalization;

namespace LifeHub.BusinessLogicLayer.ReportManagement;

/// <summary>Một khoảng nhỏ (ngày / tuần / tháng) dùng làm cột hoặc điểm trên biểu đồ.</summary>
public sealed record ReportBucket(DateOnly Start, DateOnly End, string Label);

/// <summary>
/// Khoảng thời gian báo cáo, dùng CHUNG cho mọi KPI và biểu đồ của một trang.
/// From và To đều tính trọn ngày (To là ngày cuối cùng được bao gồm, không vượt quá hôm nay).
/// Kỳ trước là khoảng liền kề có đúng cùng số ngày, dùng để so sánh.
/// </summary>
public sealed class ReportRange
{
    public string Key { get; private init; } = "30d";
    public string Label { get; private init; } = "30 ngày";
    public DateOnly From { get; private init; }
    public DateOnly To { get; private init; }
    public DateOnly PrevFrom { get; private init; }
    public DateOnly PrevTo { get; private init; }
    public int Days => To.DayNumber - From.DayNumber + 1;
    public bool Compare { get; private init; } = true;
    public DateOnly Today { get; private init; }

    // Cận dưới (gồm) và cận trên (không gồm) dạng DateTime để lọc cột DateTime trong DB
    public DateTime FromDt => From.ToDateTime(TimeOnly.MinValue);
    public DateTime ToExclusive => To.AddDays(1).ToDateTime(TimeOnly.MinValue);
    public DateTime PrevFromDt => PrevFrom.ToDateTime(TimeOnly.MinValue);
    public DateTime PrevToExclusive => PrevTo.AddDays(1).ToDateTime(TimeOnly.MinValue);

    private static string D(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string Text => $"{D(From)} – {D(To)}";
    public string PrevText => $"{D(PrevFrom)} – {D(PrevTo)}";
    public string FromIso => From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string ToIso => To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string NoteText => $"{Label}, {Text}";

    public static ReportRange Parse(string? key, string? from, string? to, bool compare, DateOnly today)
    {
        key = key?.Trim().ToLowerInvariant();
        DateOnly f, t = today;
        string k, label;
        switch (key)
        {
            case "7d": k = "7d"; label = "7 ngày"; f = today.AddDays(-6); break;
            case "90d": k = "90d"; label = "90 ngày"; f = today.AddDays(-89); break;
            case "12m": k = "12m"; label = "12 tháng"; f = today.AddMonths(-12).AddDays(1); break;
            case "custom":
                if (DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cf)
                    && DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ct)
                    && cf <= ct && ct.DayNumber - cf.DayNumber < 731)
                {
                    k = "custom"; label = "Tùy chỉnh"; f = cf; t = ct > today ? today : ct;
                    if (f > t) f = t;
                    break;
                }
                goto default;
            default: k = "30d"; label = "30 ngày"; f = today.AddDays(-29); break;
        }
        var days = t.DayNumber - f.DayNumber + 1;
        return new ReportRange
        {
            Key = k, Label = label, From = f, To = t, Today = today, Compare = compare,
            PrevTo = f.AddDays(-1), PrevFrom = f.AddDays(-days)
        };
    }

    /// <summary>Chia khoảng thành các cột: ≤ 31 ngày theo ngày, ≤ 100 ngày theo tuần (7 ngày từ ngày bắt đầu), còn lại theo tháng.</summary>
    public List<ReportBucket> Buckets()
    {
        var list = new List<ReportBucket>();
        if (Days <= 31)
            for (var d = From; d <= To; d = d.AddDays(1)) list.Add(new(d, d, d.ToString("dd/MM", CultureInfo.InvariantCulture)));
        else if (Days <= 100)
            for (var d = From; d <= To; d = d.AddDays(7))
            {
                var e = d.AddDays(6) < To ? d.AddDays(6) : To;
                list.Add(new(d, e, d.ToString("dd/MM", CultureInfo.InvariantCulture)));
            }
        else
        {
            var d = From;
            while (d <= To)
            {
                var monthEnd = new DateOnly(d.Year, d.Month, 1).AddMonths(1).AddDays(-1);
                var e = monthEnd < To ? monthEnd : To;
                list.Add(new(d, e, d.ToString("MM/yyyy", CultureInfo.InvariantCulture)));
                d = e.AddDays(1);
            }
        }
        return list;
    }
    public string BucketUnit => Days <= 31 ? "ngày" : Days <= 100 ? "tuần" : "tháng";
}
