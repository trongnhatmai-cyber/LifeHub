using System;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.StudyManagement
{
    // Dữ liệu form gửi lên Controller. Tên thuộc tính trùng với name="" của input trong _StudyModals.cshtml.

    /// <summary>Form "Thêm môn học" (bảng courses).</summary>
    public class CreateCourseInput
    {
        [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [Range(1, 10)] public int Credits { get; set; } = 3;
        public string? Instructor { get; set; }
        public string? Room { get; set; }
        public string Color { get; set; } = "#2563eb";
        [Range(0, 10)] public decimal? TargetScore { get; set; }
    }

    /// <summary>Form "Thêm bài tập / lịch thi" (bảng assignments).</summary>
    public class CreateAssignmentInput
    {
        [Required] public int CourseId { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;

        /// <summary>assignment / quiz / project / midterm / final</summary>
        [Required] public string Kind { get; set; } = "assignment";

        [Required] public DateTime DueDate { get; set; }
        [Range(0, 100)] public int WeightPercent { get; set; }
        [Range(1, 1000)] public decimal MaxScore { get; set; } = 10;
        public int? DurationMinutes { get; set; }
        public string? Location { get; set; }
    }
}