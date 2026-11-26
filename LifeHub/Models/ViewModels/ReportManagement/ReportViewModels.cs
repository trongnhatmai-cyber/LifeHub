using LifeHub.BusinessLogicLayer.ReportManagement;

namespace LifeHub.Models.ViewModels.ReportManagement
{
    // =====================================================================
    // VIEWMODEL PHÂN HỆ THỐNG KÊ & BÁO CÁO
    // Controller tính toàn bộ KPI, biểu đồ, bảng; View chỉ in ra (không tính trong Razor, không đưa Entity vào View).
    // Giá trị "--" nghĩa là chưa đủ dữ liệu để tính, không điền số giả.
    // =====================================================================

    public class ReportKpi
    {
        public string Title { get; set; } = "";
        public string Value { get; set; } = "--";
        public string Unit { get; set; } = "";
        public string Note { get; set; } = "";          // khoảng thời gian / mô tả ngắn
        public string Tooltip { get; set; } = "";       // cách tính
        public string? Delta { get; set; }              // "+12% so với kỳ trước", null nếu không có dữ liệu hợp lệ
        public string DeltaDir { get; set; } = "flat";  // up / down / flat (chỉ chỉ hướng, không phán xét tốt xấu)
        public string? LinkAction { get; set; }         // nút "Xem chi tiết" sang báo cáo tương ứng
        public string Badge { get; set; } = "";         // nhãn tiếng Anh viết hoa như các phân hệ khác
    }

    public class ChartSeries { public string Name { get; set; } = ""; public string Color { get; set; } = "#9333ea"; public List<double?> Values { get; set; } = new(); }
    public class ChartData
    {
        public string Title { get; set; } = "";
        public List<string> Labels { get; set; } = new();
        public List<ChartSeries> Series { get; set; } = new();
        public string Kind { get; set; } = "int";        // int / dec1 / money / pct (định dạng số trên trục)
        public bool Stacked { get; set; }
    }
    public class DonutSlice { public string Label { get; set; } = ""; public double Value { get; set; } public string Color { get; set; } = ""; public string ValueText { get; set; } = ""; }

    /// <summary>Một thẻ biểu đồ: đường, cột, cột chồng hoặc donut.</summary>
    public class ReportChartCard
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Type { get; set; } = "line";      // line / bar / donut
        public ChartData Data { get; set; } = new();
        public List<DonutSlice> Slices { get; set; } = new();
        public string CenterTop { get; set; } = "";
        public string CenterBottom { get; set; } = "";
        public bool Toggleable { get; set; }            // cho bật/tắt từng chuỗi bằng nút chú thích
        public bool Wide { get; set; }                  // chiếm cả hàng
        public string Empty { get; set; } = "Chưa có dữ liệu trong khoảng thời gian này.";
        public bool HasData => Type == "donut" ? Slices.Any(s => s.Value > 0) : Data.Series.Any(s => s.Values.Any(v => v.HasValue && v.Value > 0));
    }

    public class ReportCell
    {
        public string Text { get; set; } = "";
        public string? Sub { get; set; }                // dòng phụ nhỏ
        public string? Status { get; set; }             // lớp nhãn trạng thái: st-graded / st-submitted / st-overdue / st-upcoming / st-rest
        public string? Color { get; set; }              // chấm màu (phân hệ, danh mục...)
        public string? Url { get; set; }                // liên kết xem chi tiết
        public bool Right { get; set; }
        public static ReportCell T(string text, string? sub = null) => new() { Text = text, Sub = sub };
    }
    public class ReportRow { public List<ReportCell> Cells { get; set; } = new(); }

    /// <summary>Bảng dữ liệu chi tiết. PreviewRows: chỉ hiện ngần ấy dòng đầu, còn lại ẩn sau nút "Xem thêm".</summary>
    public class ReportTable
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public List<string> Headers { get; set; } = new();
        public List<int> RightColumns { get; set; } = new();
        public List<ReportRow> Rows { get; set; } = new();
        public int? PreviewRows { get; set; }
        public string Empty { get; set; } = "Chưa có dữ liệu trong khoảng thời gian này.";
    }

    public class ReportBaseViewModel
    {
        public string Section { get; set; } = "overview";   // overview / study / tasks / finance / goals / lifestyle
        public string Action { get; set; } = "Index";
        public string Eyebrow { get; set; } = "LIFEHUB ANALYTICS";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string SubColor { get; set; } = "#9333ea";  // màu phụ của nhóm báo cáo (màu phân hệ nguồn)
        public ReportRange Range { get; set; } = null!;
        public List<ReportKpi> Kpis { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public List<ReportChartCard> Charts { get; set; } = new();
        public List<ReportTable> Tables { get; set; } = new();
        public List<string> Notes { get; set; } = new();     // lưu ý cách tính / giới hạn dữ liệu
        // Bộ lọc riêng của từng trang (chỉ trang Mục tiêu & thói quen dùng)
        public string GoalStatus { get; set; } = "all";
        public string GoalType { get; set; } = "all";
        public List<string> GoalTypes { get; set; } = new();
    }
}
