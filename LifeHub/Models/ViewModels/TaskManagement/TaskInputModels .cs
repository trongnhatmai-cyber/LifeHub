using System;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    // Dữ liệu form gửi lên Controller. Tên thuộc tính trùng với name="" của input trong _TaskModals.cshtml.

    /// <summary>Form "Thêm công việc" (bảng tasks).</summary>
    public class CreateTaskInput
    {
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>todo / in_progress / review / done</summary>
        [Required] public string Status { get; set; } = "todo";

        /// <summary>urgent / high / medium / low</summary>
        [Required] public string Priority { get; set; } = "medium";

        public DateTime? Deadline { get; set; }
        public string? ProjectName { get; set; }
        [Range(1, 10000)] public int? EstimatedMinutes { get; set; }

        /// <summary>Danh sách nhãn, cách nhau bằng dấu phẩy (back-end tách ra bảng tags/task_tags).</summary>
        public string? Tags { get; set; }
    }

    /// <summary>Kéo thả thẻ sang cột khác trên Kanban.</summary>
    public class UpdateTaskStatusInput
    {
        [Required] public int Id { get; set; }
        [Required] public string Status { get; set; } = "todo";
    }
}