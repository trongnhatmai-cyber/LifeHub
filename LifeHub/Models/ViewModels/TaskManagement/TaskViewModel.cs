using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    public class TaskViewModel
    {
        public List<TaskItemViewModel> Tasks { get; set; } = new();

        // Các chỉ số được TÍNH từ danh sách công việc (không đặt cứng), nên luôn khớp với dữ liệu.
        public int TotalTasks => Tasks.Count;
        public int TodoTasksCount => Tasks.Count(t => t.Status == "todo");
        public int InProgressTasksCount => Tasks.Count(t => t.Status == "in_progress");
        public int ReviewTasksCount => Tasks.Count(t => t.Status == "review");
        public int CompletedTasksCount => Tasks.Count(t => t.Status == "done");
        public int OverdueTasksCount => Tasks.Count(t => t.IsOverdue);

        /// <summary>Danh sách dự án dùng cho bộ lọc và form thêm việc.</summary>
        public IEnumerable<string> ProjectNames => Tasks
            .Select(t => t.ProjectName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .OrderBy(n => n);
    }

    public class TaskItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>todo / in_progress / review / done (khớp cột Status của bảng tasks)</summary>
        public string Status { get; set; } = "todo";

        /// <summary>urgent / high / medium / low (viết thường, khớp cột Priority của bảng tasks)</summary>
        public string Priority { get; set; } = "medium";

        public DateTime? Deadline { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = "Cá nhân";
        public int? EstimatedMinutes { get; set; }
        public int SubtaskTotal { get; set; }
        public int SubtaskDone { get; set; }
        public int CommentCount { get; set; }
        public List<TaskTagItem> Tags { get; set; } = new();

        public bool IsDone => Status == "done";

        public bool IsOverdue => Deadline.HasValue && !IsDone && Deadline.Value.Date < DateTime.Today;

        public int SubtaskPercent => SubtaskTotal == 0 ? 0 : (int)Math.Round(100.0 * SubtaskDone / SubtaskTotal);

        public string PriorityLabel => Priority switch
        {
            "urgent" => "Khẩn cấp",
            "high" => "Cao",
            "low" => "Thấp",
            _ => "Trung bình"
        };

        public string StatusLabel => Status switch
        {
            "in_progress" => "Đang làm",
            "review" => "Chờ duyệt",
            "done" => "Đã xong",
            _ => "Cần làm"
        };

        public string EstimateLabel => !EstimatedMinutes.HasValue ? string.Empty
            : EstimatedMinutes.Value < 60 ? $"{EstimatedMinutes.Value} phút"
            : $"{Math.Round(EstimatedMinutes.Value / 60.0, 1)} giờ";
    }

    /// <summary>Nhãn hiển thị trên thẻ (từ bảng tags + task_tags).</summary>
    public class TaskTagItem
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#ea580c";
    }
}