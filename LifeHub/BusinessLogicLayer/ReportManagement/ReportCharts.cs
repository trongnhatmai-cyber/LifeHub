using System.Globalization;
using System.Net;
using System.Text;
using LifeHub.Models.ViewModels.ReportManagement;

namespace LifeHub.BusinessLogicLayer.ReportManagement;

/// <summary>
/// Vẽ biểu đồ SVG tối giản (không gradient, không thư viện ngoài) từ số liệu đã tính sẵn.
/// Trả về chuỗi SVG; View in bằng Html.Raw. Mọi nhãn đều được HTML-encode.
/// Màu của từng chuỗi truyền qua --rc-color để CSS dùng; trục, lưới dùng lớp rc-* nên tự đổi theo theme sáng / tối.
/// </summary>
public static class ReportCharts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double v) => v.ToString("0.##", Inv);
    private static string E(string s) => WebUtility.HtmlEncode(s ?? "");

    private static double NiceMax(double v, bool integer)
    {
        if (v <= 0) return integer ? 4 : 1;
        var exp = Math.Floor(Math.Log10(v));
        var f = v / Math.Pow(10, exp);
        var nice = f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10;
        var max = nice * Math.Pow(10, exp);
        return integer && max < 4 ? 4 : max;
    }

    public static string Format(double v, string kind)
    {
        switch (kind)
        {
            case "money":
                var a = Math.Abs(v);
                if (a >= 1e9) return (v / 1e9).ToString("0.#", Inv) + " tỷ";
                if (a >= 1e6) return (v / 1e6).ToString("0.#", Inv) + " tr";
                if (a >= 1e3) return (v / 1e3).ToString("0.#", Inv) + "k";
                return v.ToString("0", Inv);
            case "dec1": return v.ToString("0.#", Inv);
            case "pct": return v.ToString("0", Inv) + "%";
            default: return Math.Round(v).ToString("0", Inv);
        }
    }

    private static void Grid(StringBuilder sb, double w, double h, double l, double r, double t, double b, double max, string kind)
    {
        for (var k = 0; k <= 4; k++)
        {
            var y = t + (1 - k / 4.0) * (h - t - b);
            sb.Append($"""<line class="rc-grid" x1="{F(l)}" x2="{F(w - r)}" y1="{F(y)}" y2="{F(y)}" />""");
            sb.Append($"""<text class="rc-axis" x="{F(l - 6)}" y="{F(y + 4)}" text-anchor="end">{E(Format(max * k / 4.0, kind))}</text>""");
        }
    }

    private static void XLabels(StringBuilder sb, ChartData d, Func<int, double> x, double h, int step)
    {
        var n = d.Labels.Count;
        for (var i = 0; i < n; i += step)
        {
            var anchor = n > 1 && i == 0 ? "start" : "middle";
            sb.Append($"""<text class="rc-axis" x="{F(x(i))}" y="{F(h - 8)}" text-anchor="{anchor}">{E(d.Labels[i])}</text>""");
        }
    }

    /// <summary>Biểu đồ đường. Điểm không có dữ liệu (null) làm đứt đường, không bị tính là 0.</summary>
    public static string Line(ChartData d, double w = 640, double h = 240)
    {
        const double L = 46, R = 14, T = 12, B = 30;
        var n = d.Labels.Count;
        var values = d.Series.SelectMany(s => s.Values).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var max = NiceMax(values.Count == 0 ? 0 : values.Max(), d.Kind == "int");
        double X(int i) => n <= 1 ? L + (w - L - R) / 2 : L + i * (w - L - R) / (n - 1);
        double Y(double v) => T + (1 - v / max) * (h - T - B);

        var sb = new StringBuilder();
        sb.Append($"""<svg class="rc" viewBox="0 0 {F(w)} {F(h)}" role="img" aria-label="{E(d.Title)}">""");
        Grid(sb, w, h, L, R, T, B, max, d.Kind);
        for (var si = 0; si < d.Series.Count; si++)
        {
            var s = d.Series[si];
            sb.Append($"""<g class="rc-series" data-series="{si}" style="--rc-color:{E(s.Color)}">""");
            var run = new List<int>();
            void Flush()
            {
                if (run.Count >= 2) sb.Append($"""<polyline class="rc-line" points="{string.Join(" ", run.Select(i => F(X(i)) + "," + F(Y(s.Values[i]!.Value))))}" />""");
                run.Clear();
            }
            for (var i = 0; i < n; i++) { if (s.Values[i].HasValue) run.Add(i); else Flush(); }
            Flush();
            if (n <= 31)
                for (var i = 0; i < n; i++)
                    if (s.Values[i].HasValue)
                        sb.Append($"""<circle class="rc-dot" cx="{F(X(i))}" cy="{F(Y(s.Values[i]!.Value))}" r="3"><title>{E(d.Labels[i])}, {E(s.Name)}: {E(Format(s.Values[i]!.Value, d.Kind))}</title></circle>""");
            sb.Append("</g>");
        }
        XLabels(sb, d, X, h, Math.Max(1, (int)Math.Ceiling(n / 7.0)));
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Biểu đồ cột: nhóm cạnh nhau, hoặc chồng nếu d.Stacked.</summary>
    public static string Bar(ChartData d, double w = 640, double h = 240)
    {
        const double L = 46, R = 14, T = 12, B = 30;
        var n = d.Labels.Count;
        var m = Math.Max(1, d.Series.Count);
        double Val(int si, int i) => d.Series[si].Values[i] ?? 0;
        var maxVal = n == 0 ? 0 : Enumerable.Range(0, n).Max(i => d.Stacked ? Enumerable.Range(0, d.Series.Count).Sum(si => Val(si, i)) : Enumerable.Range(0, d.Series.Count).Max(si => Val(si, i)));
        var max = NiceMax(maxVal, d.Kind == "int");
        var band = (w - L - R) / Math.Max(1, n);
        double Y(double v) => T + (1 - v / max) * (h - T - B);
        double Cx(int i) => L + band * (i + 0.5);

        var sb = new StringBuilder();
        sb.Append($"""<svg class="rc" viewBox="0 0 {F(w)} {F(h)}" role="img" aria-label="{E(d.Title)}">""");
        Grid(sb, w, h, L, R, T, B, max, d.Kind);
        var barW = d.Stacked ? Math.Min(band * 0.6, 44) : Math.Min(band * 0.76 / m, 26);
        for (var i = 0; i < n; i++)
        {
            double acc = 0;
            for (var si = 0; si < d.Series.Count; si++)
            {
                var v = Val(si, i);
                if (v <= 0) continue;
                var s = d.Series[si];
                double x, y0, y1;
                if (d.Stacked) { x = Cx(i) - barW / 2; y0 = Y(acc + v); y1 = Y(acc); acc += v; }
                else { x = Cx(i) - barW * m / 2 + si * barW; y0 = Y(v); y1 = Y(0); }
                sb.Append($"""<rect class="rc-bar" x="{F(x)}" y="{F(y0)}" width="{F(Math.Max(1, barW - (d.Stacked ? 0 : 2)))}" height="{F(Math.Max(1, y1 - y0))}" rx="2" style="--rc-color:{E(s.Color)}"><title>{E(d.Labels[i])}, {E(s.Name)}: {E(Format(v, d.Kind))}</title></rect>""");
            }
        }
        XLabels(sb, d, Cx, h, Math.Max(1, (int)Math.Ceiling(n / 8.0)));
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Vòng donut chuẩn hóa chu vi = 100; chữ ở giữa do View đặt bằng HTML.</summary>
    public static string Donut(List<DonutSlice> slices)
    {
        var total = slices.Sum(s => s.Value);
        var sb = new StringBuilder();
        sb.Append("""<svg class="rc-donut" viewBox="0 0 36 36" role="img" aria-label="Biểu đồ phân bổ"><circle class="rc-donut-bg" cx="18" cy="18" r="15.9155" />""");
        double offset = 0;
        foreach (var s in slices)
        {
            if (total <= 0 || s.Value <= 0) continue;
            var p = s.Value * 100.0 / total;
            sb.Append($"""<circle class="rc-donut-seg" cx="18" cy="18" r="15.9155" stroke="{E(s.Color)}" stroke-dasharray="{F(p)} {F(100 - p)}" stroke-dashoffset="{F(-offset)}"><title>{E(s.Label)}: {E(s.ValueText)}</title></circle>""");
            offset += p;
        }
        sb.Append("</svg>");
        return sb.ToString();
    }
}
