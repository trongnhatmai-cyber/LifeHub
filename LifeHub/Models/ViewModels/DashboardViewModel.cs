using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = "Minh";
        public string TodayDateFormatted { get; set; } = string.Empty;
        public int WeeklyMomentumPercent { get; set; } = 68;

        // 4 Thẻ chỉ số tổng hợp
        public int PendingTasksCount { get; set; } = 4;
        public int ActiveProjectsCount { get; set; } = 3;
        public decimal CurrentGpa { get; set; } = 3.42m;
        public decimal TotalBalance { get; set; } = 12200000m;

        // Danh sách công việc hôm nay
        public List<DashboardTaskItem> TodayTasks { get; set; } = new();

        // Sự kiện lịch sắp tới
        public List<DashboardEventItem> UpcomingEvents { get; set; } = new();

        // Tiến độ mục tiêu nổi bật
        public List<DashboardGoalItem> Goals { get; set; } = new();

        // Thói quen hôm nay
        public List<DashboardHabitItem> Habits { get; set; } = new();
    }

    public class DashboardTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = "Trung bình"; // Khẩn cấp, Cao, Trung bình, Thấp
        public string Category { get; set; } = "Học tập";
        public string DueTime { get; set; } = "Hôm nay";
        public bool IsCompleted { get; set; }
    }

    public class DashboardEventItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Tag { get; set; } = "Lịch học";
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
}
