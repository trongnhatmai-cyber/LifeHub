using System;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.GoalManagement
{
    // Dữ liệu form gửi lên GoalController. Tên thuộc tính trùng name="" của input trong _GoalModals.cshtml.

    public class SaveGoalInput
    {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [StringLength(2000)] public string? Description { get; set; }
        [Required, StringLength(30)] public string Category { get; set; } = "Cá nhân";
        /// <summary>short | long</summary>
        [Required] public string GoalType { get; set; } = "short";
        public DateOnly? StartDate { get; set; }
        public DateOnly? TargetDate { get; set; }
        /// <summary>active | paused | completed</summary>
        public string Status { get; set; } = "active";
        [Range(0, 100)] public int ProgressPercent { get; set; }
    }

    public class SaveKeyResultInput
    {
        public int Id { get; set; }
        [Required] public int GoalId { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [Range(0.01, 99999999)] public decimal TargetValue { get; set; } = 1;
        [Range(0, 99999999)] public decimal CurrentValue { get; set; }
        [StringLength(30)] public string? Unit { get; set; }
    }

    public class UpdateKeyResultValueInput
    {
        [Required] public int Id { get; set; }
        [Range(0, 99999999)] public decimal CurrentValue { get; set; }
    }

    public class SaveReviewInput
    {
        [Required] public int Id { get; set; }
        [StringLength(4000)] public string? ReviewNote { get; set; }
    }
}
