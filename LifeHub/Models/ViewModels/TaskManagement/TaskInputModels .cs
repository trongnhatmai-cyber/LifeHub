
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    // ViewModel nhận dữ liệu từ các form thuộc module Task.
    // Dùng chung cho tạo công việc, cập nhật trạng thái và bình luận.
    public class TaskInputModels
    {
        // ===== THÔNG TIN CÔNG VIỆC =====

        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công việc.")]
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }
        
        public string Status { get; set; } = "todo";
        public string Priority { get; set; } = "medium";
        public DateTime? Deadline { get; set; }
        public int? EstimatedMinutes { get; set; }

        // ===== PHÂN LOẠI CÔNG VIỆC =====
        public string TaskType { get; set; } = "personal";
        public int? CategoryId { get; set; }
        public int? ProjectId { get; set; }
        public int? AssignedTo { get; set; }
        public string? Tags { get; set; }

        // ===== BÌNH LUẬN =====

        public int TaskId { get; set; }
        public string? Content { get; set; }
    }
}
