using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.HabitManagement
{
    // ViewModel cho 5 trang Habit. Mọi con số do Controller tính sẵn, View chỉ in ra.
    // Không sửa model entity (Habit, HabitLog): mọi thông tin bổ sung nằm ở ViewModel.

    public static class HabitFrequency
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Monthly = "monthly";

        /// <summary>Chuẩn hóa giá trị cột frequency (varchar) về daily | weekly | monthly.</summary>
        public static string Normalize(string? raw)
        {
            var v = (raw ?? "").Trim().ToLowerInvariant();
            if (v.Contains("week") || v.Contains("tuần")) return Weekly;
            if (v.Contains("month") || v.Contains("tháng")) return Monthly;
            return Daily;
        }

        public static string Label(string key) => key switch { Weekly => "Hằng tuần", Monthly => "Hằng tháng", _ => "Hằng ngày" };

        /// <summary>
        /// Số lần thực hiện DỰ KIẾN trong một khoảng ngày, theo Frequency và TargetPerPeriod:
        /// daily = số ngày × target; weekly = ngày/7 × target; monthly = ngày/30 × target.
        /// Tỷ lệ hoàn thành = số lần đã làm / số lần dự kiến (không lấy log chia số thói quen).
        /// </summary>
        public static double Expected(string key, int targetPerPeriod, int days)
        {
            var t = Math.Max(1, targetPerPeriod);
            if (days <= 0) return 0;
            return key switch { Weekly => days / 7.0 * t, Monthly => days / 30.0 * t, _ => days * (double)t };
        }

        public static int Rate(double done, double expected) => expected <= 0 ? 0 : (int)Math.Clamp(Math.Round(done / expected * 100), 0, 100);
    }

    public class HabitGoalOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    public class HabitCardItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Color { get; set; } = "#4f46e5";
        public string Frequency { get; set; } = HabitFrequency.Daily;   // daily | weekly | monthly
        public int TargetPerPeriod { get; set; } = 1;
        public string ReminderTime { get; set; } = "";                  // HH:mm hoặc rỗng
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool Active { get; set; } = true;
        public int? GoalId { get; set; }
        public string GoalTitle { get; set; } = "";

        // Trạng thái của ngày đang xem (DateKey)
        public string DateKey { get; set; } = "";                       // yyyy-MM-dd
        public bool DoneOnDate { get; set; }
        public int CountOnDate { get; set; }
        public string NoteOnDate { get; set; } = "";

        public int Rate7 { get; set; }

        // Tiến độ trong kỳ (daily = trong ngày, weekly = 7 ngày, monthly = 30 ngày kết thúc ở DateKey)
        public int PeriodDone { get; set; }
        public string PeriodUnit => Frequency == HabitFrequency.Weekly ? "tuần" : Frequency == HabitFrequency.Monthly ? "tháng" : "ngày";
        public string PeriodLabel => $"{PeriodDone}/{TargetPerPeriod} lần trong {PeriodUnit}";

        /// <summary>done | partial | todo. Mỗi trạng thái luôn có nhãn chữ, không chỉ dựa vào màu.</summary>
        public string State { get; set; } = "todo";
        public string StateLabel => State == "done" ? "Đã hoàn thành" : State == "partial" ? "Đang thực hiện" : "Chưa thực hiện";
        public string StateCss => State == "done" ? "graded" : State == "partial" ? "submitted" : "upcoming";

        public string FrequencyLabel => HabitFrequency.Label(Frequency);
        public string TargetLabel => TargetPerPeriod <= 1 ? FrequencyLabel : $"{FrequencyLabel}, {TargetPerPeriod} lần/kỳ";
        public string StatusLabel => Active ? "Đang bật" : "Tạm dừng";
        public string StatusCss => Active ? "submitted" : "upcoming";
    }

    /// <summary>Một ô ngày trong heatmap hoặc lịch streak. Level 0–4 (0 = không có, 4 = hoàn thành cao nhất).</summary>
    public class HabitDayCell
    {
        public DateOnly Date { get; set; }
        public int Level { get; set; }
        public int Done { get; set; }
        public int Total { get; set; }
        public bool IsToday { get; set; }
        public string Tip => Total <= 0 ? $"{Date:dd/MM}: chưa có thói quen" : $"{Date:dd/MM}: {Done}/{Total} thói quen";
    }

    // ---------------- 01 Overview ----------------
    public class HabitOverviewViewModel
    {
        public string DateLabel { get; set; } = "";
        public int ActiveCount { get; set; }
        public int DoneToday { get; set; }
        public int RateToday { get; set; }          // = DoneToday / ActiveCount
        public int BestStreak { get; set; }
        public List<HabitCardItem> Today { get; set; } = new();
        public List<HabitCardItem> TopStreaks { get; set; } = new();
        public List<HabitCardItem> Attention { get; set; } = new();   // chuỗi đang có nhưng hôm nay chưa check-in
        public List<HabitDayCell> Heatmap { get; set; } = new();      // 30 ngày gần nhất
    }

    // ---------------- 02 List ----------------
    public class HabitListViewModel
    {
        public string Status { get; set; } = "all";   // all | active | paused
        public string Frequency { get; set; } = "all"; // all | daily | weekly | monthly
        public string Sort { get; set; } = "name";     // name | newest | streak
        public string? Q { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int PausedCount { get; set; }
        public List<HabitCardItem> Items { get; set; } = new();
        public List<HabitGoalOption> Goals { get; set; } = new();
    }

    // ---------------- 03 Tracking ----------------
    public class HabitTrackingViewModel
    {
        public DateOnly Date { get; set; }
        public bool IsToday { get; set; }
        public string PrevKey { get; set; } = "";
        public string? NextKey { get; set; }          // null khi đang ở hôm nay (không check-in ngày tương lai)
        public int DoneCount { get; set; }
        public int Total { get; set; }
        public List<HabitCardItem> Items { get; set; } = new();
        public List<HabitGoalOption> Goals { get; set; } = new();
    }

    // ---------------- 04 Streak ----------------
    public class HabitStreakItem
    {
        public HabitCardItem Habit { get; set; } = new();
        public List<HabitDayCell> Days { get; set; } = new();   // 30 ngày: Level 4 = đã làm, 0 = chưa
    }

    public class HabitStreakViewModel
    {
        public int BestCurrent { get; set; }
        public int BestEver { get; set; }
        public int Streaking { get; set; }       // số thói quen có streak >= 1
        public int Broken { get; set; }          // số thói quen đang bật nhưng streak = 0
        public List<HabitStreakItem> Items { get; set; } = new();
    }

    // ---------------- 05 Statistics ----------------
    public class HabitStatRow
    {
        public int HabitId { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public string Title { get; set; } = "";
        public string Color { get; set; } = "#4f46e5";
        public string FrequencyLabel { get; set; } = "";
        public int Done { get; set; }
        public double Expected { get; set; }
        public int Rate { get; set; }
    }

    public class HabitTrendPoint
    {
        public string Label { get; set; } = "";
        public int Rate { get; set; }
    }

    public class HabitMissedItem
    {
        public string HabitTitle { get; set; } = "";
        public string Color { get; set; } = "#4f46e5";
        public DateOnly Date { get; set; }
    }

    public class HabitStatisticsViewModel
    {
        public int Range { get; set; } = 30;                // 7 | 30 | 90
        public int OverallRate { get; set; }
        public int TotalCompletions { get; set; }
        public int MissedDays { get; set; }                 // chỉ tính thói quen hằng ngày
        public int HabitCount { get; set; }
        public int BestCurrent { get; set; }
        public int BestEver { get; set; }
        public List<HabitTrendPoint> Trend { get; set; } = new();
        public List<HabitStatRow> Rows { get; set; } = new();          // từng thói quen, tỷ lệ cao -> thấp
        public List<HabitMissedItem> Missed { get; set; } = new();
        public List<HabitDayCell> Heatmap { get; set; } = new();
        public List<string> Insights { get; set; } = new();            // chỉ sinh từ dữ liệu thật, quy tắc thống kê đơn giản
    }
}
