using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels
{
    // Giữ nguyên tên lớp và thuộc tính cũ. Phần thêm mới được đánh dấu [MỚI].
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string TodayDateFormatted { get; set; } = string.Empty;
        public int WeeklyMomentumPercent { get; set; }

        // 4 thẻ chỉ số tổng hợp
        public int PendingTasksCount { get; set; }
        public int ActiveProjectsCount { get; set; }
        public decimal? CurrentGpa { get; set; }
        public decimal? TotalBalance { get; set; }

        // Danh sách công việc hôm nay
        public List<DashboardTaskItem> TodayTasks { get; set; } = new();

        // Sự kiện lịch sắp tới
        public List<DashboardEventItem> UpcomingEvents { get; set; } = new();

        // Tiến độ mục tiêu nổi bật
        public List<DashboardGoalItem> Goals { get; set; } = new();

        // Thói quen hôm nay
        public List<DashboardHabitItem> Habits { get; set; } = new();

        // ----- [MỚI] Điểm năng suất (bảng productivity_scores) -----
        public int ProductivityScore { get; set; }                       // hôm nay, 0-100
        public int ProductivityChange { get; set; }                      // chênh lệch so với hôm qua
        public int FocusMinutesToday { get; set; }                       // tổng phút Pomodoro hôm nay
        public List<DashboardChartPoint> ProductivityWeek { get; set; } = new();

        // ----- [MỚI] Tài chính tháng này -----
        public decimal MonthIncome { get; set; }
        public decimal MonthExpense { get; set; }
        public decimal MonthlyBudget { get; set; }
        public int BudgetUsedPercent => MonthlyBudget > 0 ? (int)Math.Min(999, Math.Round(MonthExpense / MonthlyBudget * 100)) : 0;

        // ----- [MỚI] Học tập -----
        public string NextExamName { get; set; } = string.Empty;
        public int NextExamDays { get; set; }
        public int DueSoonAssignments { get; set; }
        public bool HasNextExam => !string.IsNullOrEmpty(NextExamName);

        // ----- [MỚI] Thông báo và thành tựu -----
        public int UnreadNotificationsCount { get; set; }
        public List<DashboardNotificationItem> LatestNotifications { get; set; } = new();
        public List<DashboardAchievementItem> RecentAchievements { get; set; } = new();

        // ----- [MỚI] Widget người dùng muốn hiển thị (user_settings.DashboardLayout). Rỗng = hiện tất cả. -----
        // Khóa: productivity, tasks, goals, events, habits, notifications, finance, study, achievements
        public List<string> VisibleWidgets { get; set; } = new();
        public bool IsWidgetVisible(string key) => VisibleWidgets.Count == 0 || VisibleWidgets.Contains(key);

        // ----- [MỚI] 4 thẻ "Hôm nay của bạn": tự tính từ danh sách có sẵn, riêng deadline cần Controller gán -----
        public int TodayOpenTasksCount => TodayTasks.Count(t => !t.IsCompleted);
        public int TodayEventsCount => UpcomingEvents.Count;
        public int HabitsRemainingToday => Habits.Count(h => !h.IsCompletedToday);
        public int OverdueTasksCount { get; set; }                       // quá hạn, chưa hoàn thành
        public int DueSoonTasksCount { get; set; }                       // hạn trong 48 giờ tới

        // ----- [MỚI] Weekly Snapshot (cần Controller tổng hợp theo tuần; chưa có thì View hiện trạng thái rỗng) -----
        public int WeeklyCompletedTasks { get; set; }
        public int WeeklyFocusMinutes { get; set; }
        public int WeeklyGoalProgressPercent => Goals.Count > 0 ? (int)Math.Round(Goals.Average(g => g.ProgressPercent)) : 0;
        public bool HasWeeklyData => ProductivityWeek.Count > 0 || WeeklyCompletedTasks > 0 || WeeklyFocusMinutes > 0;

        // ----- [MỚI] Module Snapshots -----
        public int ActiveCoursesCount { get; set; }
        public int ProjectProgressPercent { get; set; }
        public int BestHabitStreak => Habits.Count > 0 ? Habits.Max(h => h.StreakDays) : 0;

        // ----- [MỚI] Recent Activity -----
        public List<DashboardActivityItem> RecentActivities { get; set; } = new();
    }

    public class DashboardTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DueTime { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }

        // [MỚI] todo | in_progress | completed. Mặc định todo để Controller cũ vẫn chạy.
        public string Status { get; set; } = "todo";
        public bool IsOverdue { get; set; }

        public string PriorityLabel => Priority switch
        {
            "urgent" => "Khẩn cấp",
            "high" => "Cao",
            "low" => "Thấp",
            _ => "Trung bình"
        };

        public string StatusKey => IsCompleted ? "graded" : IsOverdue ? "overdue" : Status == "in_progress" ? "submitted" : "upcoming";
        public string StatusLabel => IsCompleted ? "Hoàn thành" : IsOverdue ? "Quá hạn" : Status == "in_progress" ? "Đang làm" : "Chưa làm";
        public int PriorityWeight => Priority switch { "urgent" => 0, "high" => 1, "medium" => 2, _ => 3 };
    }

    public class DashboardEventItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

        // [MỚI] "Hôm nay", "Ngày mai", "28/11"... Rỗng thì View coi là hôm nay.
        public string DayLabel { get; set; } = string.Empty;
    }

    public class DashboardGoalItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class DashboardHabitItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int StreakDays { get; set; }
        public bool IsCompletedToday { get; set; }
    }

    /// <summary>[MỚI] Một cột trên biểu đồ năng suất 7 ngày.</summary>
    public class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;   // T2, T3...
        public int Value { get; set; }                       // 0-100
        public bool IsToday { get; set; }
    }

    /// <summary>[MỚI] Thông báo mới nhất hiển thị trên Dashboard (bảng notifications).</summary>
    public class DashboardNotificationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }

    /// <summary>[MỚI] Huy hiệu vừa đạt (bảng user_achievements).</summary>
    public class DashboardAchievementItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "★";
        public string Accent { get; set; } = "goal";
        public int Points { get; set; }
    }

    /// <summary>[MỚI] Một dòng hoạt động gần đây (hoàn thành việc, cập nhật mục tiêu, ghi nhận thói quen...).</summary>
    public class DashboardActivityItem
    {
        public string Kind { get; set; } = "task";           // task | goal | habit | finance | project
        public string Text { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;     // đường dẫn tuỳ chọn
    }
}
