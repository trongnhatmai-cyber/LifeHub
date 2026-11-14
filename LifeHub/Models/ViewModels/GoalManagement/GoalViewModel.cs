using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.GoalManagement
{
    public class GoalViewModel
    {
        public int TotalGoals { get; set; } = 4;
        public int ActiveHabitsCount { get; set; } = 6;
        public int LongestStreakDays { get; set; } = 21;
        public int OverallProgressPercent { get; set; } = 68;

        public List<GoalItemViewModel> Goals { get; set; } = new();
        public List<HabitItemViewModel> Habits { get; set; } = new();
    }

    public class GoalItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "Học tập"; // Học tập, Sự nghiệp, Sức khỏe, Tài chính, Cá nhân
        public string GoalType { get; set; } = "Ngắn hạn"; // Ngắn hạn, Dài hạn
        public int ProgressPercent { get; set; }
        public DateTime TargetDate { get; set; }
        public List<KeyResultItemViewModel> KeyResults { get; set; } = new();
    }

    public class KeyResultItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class HabitItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Frequency { get; set; } = "Hàng ngày";
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool IsCompletedToday { get; set; }
    }
}
