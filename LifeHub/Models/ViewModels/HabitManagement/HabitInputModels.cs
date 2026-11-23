using System;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.HabitManagement
{
    // Dữ liệu form (chuẩn bị sẵn cho khi có action POST). Tên thuộc tính trùng name="" trong _HabitModals.cshtml.

    public class SaveHabitInput
    {
        public int Id { get; set; }
        [Required, StringLength(150)] public string Title { get; set; } = "";
        /// <summary>daily | weekly | monthly (cột varchar: lưu mã ASCII)</summary>
        [Required] public string Frequency { get; set; } = "daily";
        [Range(1, 31)] public int TargetPerPeriod { get; set; } = 1;
        public TimeOnly? ReminderTime { get; set; }
        public string ColorCode { get; set; } = "#4f46e5";
        public int? GoalId { get; set; }
        public bool Active { get; set; } = true;
    }

    public class HabitCheckInInput
    {
        [Required] public int HabitId { get; set; }
        [Required] public DateOnly CheckDate { get; set; }
        public bool IsCompleted { get; set; } = true;
        [Range(1, 1000)] public int CountValue { get; set; } = 1;
        [StringLength(200)] public string? Note { get; set; }
    }
}
