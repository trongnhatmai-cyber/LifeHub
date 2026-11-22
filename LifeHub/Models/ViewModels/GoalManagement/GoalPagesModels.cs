using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.GoalManagement
{
    // ViewModel cho 5 trang Goal. Mọi con số do Controller tính sẵn, View chỉ in ra.

    public static class GoalStatusKeys
    {
        public const string Active = "active";
        public const string Paused = "paused";
        public const string Completed = "completed";

        public static string Label(string key) => key switch
        {
            Completed => "Hoàn thành",
            Paused => "Tạm dừng",
            _ => "Đang thực hiện"
        };

        // Khóa CSS dùng chung với nhãn .status: st-graded / st-submitted / st-overdue / st-upcoming
        public static string Css(string key, bool overdue) => key switch
        {
            Completed => "graded",
            Paused => "upcoming",
            _ => overdue ? "overdue" : "submitted"
        };
    }

    public static class GoalCategories
    {
        // Khóa ASCII (lưu trong cột varchar) -> nhãn hiển thị. Dữ liệu cũ có giá trị khác vẫn hiển thị nguyên văn.
        public static readonly Dictionary<string, string> Labels = new()
        {
            ["study"] = "Học tập",
            ["career"] = "Sự nghiệp",
            ["health"] = "Sức khỏe",
            ["finance"] = "Tài chính",
            ["personal"] = "Cá nhân"
        };

        public static string Label(string? key) =>
            string.IsNullOrWhiteSpace(key) ? "Chưa phân loại" : Labels.TryGetValue(key, out var v) ? v : key;
    }

    public class GoalCardItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string GoalType { get; set; } = "";          // short | long
        public string Status { get; set; } = "active";      // active | paused | completed
        public int Progress { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? TargetDate { get; set; }
        public bool IsCompleted { get; set; }
        public string? ReviewNote { get; set; }
        public int KeyResultCount { get; set; }
        public int KeyResultDone { get; set; }
        public bool HasKeyResults => KeyResultCount > 0;

        public string CategoryLabel => GoalCategories.Label(Category);
        public string GoalTypeLabel => GoalType == "long" ? "Dài hạn" : "Ngắn hạn";
        public string StatusLabel => GoalStatusKeys.Label(Status);
        public string StatusCss => GoalStatusKeys.Css(Status, IsOverdue);
        public bool IsOverdue => Status != GoalStatusKeys.Completed && TargetDate.HasValue && TargetDate.Value < DateOnly.FromDateTime(DateTime.Today);

        /// <summary>Số ngày còn lại (âm = quá hạn). Null nếu không có hạn.</summary>
        public int? DaysLeft => TargetDate.HasValue ? TargetDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber : null;
        public string DueHint => DaysLeft switch
        {
            null => "Chưa đặt hạn",
            < 0 => $"Quá hạn {-DaysLeft} ngày",
            0 => "Hạn là hôm nay",
            var d => $"Còn {d} ngày"
        };

        // Mức tiến độ cho trang Progress
        public string Band => Progress >= 100 ? "done" : Progress >= 70 ? "high" : Progress >= 30 ? "mid" : "low";
    }

    public class KeyResultRow
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public string Title { get; set; } = "";
        public decimal Current { get; set; }
        public decimal Target { get; set; }
        public string Unit { get; set; } = "";
        public bool IsCompleted { get; set; }
        // 0–100, đã xử lý Target <= 0 và giới hạn khoảng
        public int Percent => Target <= 0 ? 0 : (int)Math.Clamp(Math.Round(Current / Target * 100m, MidpointRounding.AwayFromZero), 0m, 100m);
        public decimal PercentExact => Target <= 0 ? 0 : Math.Clamp(Math.Round(Current / Target * 100m, 1), 0m, 100m);
    }

    public class GoalKeyResultGroup
    {
        public GoalCardItem Goal { get; set; } = new();
        public List<KeyResultRow> Items { get; set; } = new();
    }

    // ---------------- 01 Overview ----------------
    public class GoalOverviewViewModel
    {
        public int Total { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int KeyResultCount { get; set; }
        public int AverageProgress { get; set; }
        public List<GoalCardItem> Attention { get; set; } = new();   // gần hạn / quá hạn
        public List<GoalCardItem> Recent { get; set; } = new();      // mới tạo (chưa có UpdatedAt)
        public List<KeyResultRow> TopKeyResults { get; set; } = new();
        public Dictionary<string, int> ByCategory { get; set; } = new();
    }

    // ---------------- 02 List ----------------
    public class GoalListViewModel
    {
        public string Status { get; set; } = "all";
        public string Category { get; set; } = "all";
        public string Type { get; set; } = "all";
        public string Sort { get; set; } = "due";
        public string? Q { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int PausedCount { get; set; }
        public int CompletedCount { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<GoalCardItem> Items { get; set; } = new();
    }

    // ---------------- 03 Key Results ----------------
    public class GoalKeyResultViewModel
    {
        public int Total { get; set; }
        public int DoneCount { get; set; }
        public int InProgressCount => Total - DoneCount;
        public int? GoalId { get; set; }
        public List<GoalCardItem> Goals { get; set; } = new();       // cho ô chọn trong modal / chip lọc
        public List<GoalKeyResultGroup> Groups { get; set; } = new();
    }

    // ---------------- 04 Progress ----------------
    public class GoalProgressViewModel
    {
        public int Average { get; set; }
        public int LowCount { get; set; }
        public int MidCount { get; set; }
        public int HighCount { get; set; }
        public int DoneCount { get; set; }
        public int Total { get; set; }
        public List<GoalCardItem> Leading { get; set; } = new();     // đang tiến triển tốt
        public List<GoalCardItem> Behind { get; set; } = new();      // chưa đạt kỳ vọng
        public List<GoalCardItem> DueSoon { get; set; } = new();     // gần đến hạn
    }

    // ---------------- 05 Review ----------------
    public class GoalReviewViewModel
    {
        public List<GoalCardItem> Goals { get; set; } = new();
        public GoalCardItem? Selected { get; set; }
        public List<KeyResultRow> KeyResults { get; set; } = new();
        public int ReviewedCount { get; set; }
    }
}
