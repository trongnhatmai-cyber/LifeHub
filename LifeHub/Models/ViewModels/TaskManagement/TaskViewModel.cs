
using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    /// <summary>
    /// Thông tin một công việc dùng chung trong các trang.
    /// </summary>
    public class TaskItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>todo / in_progress / review / done</summary>
        public string Status { get; set; } = "todo";

        /// <summary>urgent / high / medium / low</summary>
        public string Priority { get; set; } = "medium";

        public DateTime? Deadline { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string CourseCode { get; set; } = string.Empty;

        public string AssignedTo { get; set; } = "Bạn";

        public string AssignedBy { get; set; } = string.Empty;

        public int? EstimatedMinutes { get; set; }

        public int SubtaskTotal { get; set; }

        public int SubtaskDone { get; set; }

        public int? CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public List<TaskTagItem> Tags { get; set; } = new();

        public List<TaskCommentItem> Comments { get; set; } = new();

        public int CommentCount => Comments.Count;

        public bool IsProjectTask => ProjectId.HasValue;

        public bool IsAssignedToMe => !string.IsNullOrEmpty(AssignedBy);

        public bool IsDone => Status == "done";

        public bool IsOverdue =>
            Deadline.HasValue &&
            !IsDone &&
            Deadline.Value.Date < DateTime.Today;

        public bool IsDueSoon =>
            Deadline.HasValue &&
            !IsDone &&
            Deadline.Value.Date >= DateTime.Today &&
            Deadline.Value.Date <= DateTime.Today.AddDays(7);

        public int SubtaskPercent =>
            SubtaskTotal == 0
                ? 0
                : (int)Math.Round(100.0 * SubtaskDone / SubtaskTotal);

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
            "done" => "Đã hoàn thành",
            _ => "Cần làm"
        };

        public string EstimateLabel =>
            !EstimatedMinutes.HasValue
                ? string.Empty
                : EstimatedMinutes.Value < 60
                    ? $"{EstimatedMinutes.Value} phút"
                    : $"{Math.Round(EstimatedMinutes.Value / 60.0, 1)} giờ";
    }

    /// <summary>
    /// Nhãn hiển thị trên thẻ công việc.
    /// </summary>
    public class TaskTagItem
    {
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#ea580c";
    }

    /// <summary>
    /// Bình luận thuộc một công việc.
    /// </summary>
    public class TaskCommentItem
    {
        public int Id { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Thông tin dự án dùng trong bộ lọc và form.
    /// </summary>
    public class TaskProjectOption
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<TaskMemberOption> Members { get; set; } = new();
    }

    /// <summary>
    /// Thành viên thuộc dự án.
    /// </summary>
    public class TaskMemberOption
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Role { get; set; } = "member";
    }

    /// <summary>
    /// Thông tin danh mục dùng trong bộ lọc.
    /// </summary>
    public class TaskCategoryOption
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#8B9CF6";
    }

    /// <summary>
    /// Một điểm dữ liệu trên biểu đồ hoàn thành.
    /// </summary>
    public class TaskCompletionTrendItem
    {
        public DateTime Date { get; set; }

        public string Label => Date.ToString("dd/MM");

        public int CompletedCount { get; set; }
    }

    /// <summary>
    /// Thông tin một danh mục dùng để hiển thị.
    /// </summary>
    public class TaskCategoryItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Color { get; set; } = "#8B9CF6";

        public string Icon { get; set; } = "folder";

        public int TaskCount { get; set; }
    }
}
