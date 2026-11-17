using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    public class TaskViewModel
    {
        public List<TaskItemViewModel> Tasks { get; set; } = new();

        /// <summary>Các dự án mà người dùng là thành viên (bảng projects + project_members). Dùng cho bộ lọc và form thêm việc.</summary>
        public List<TaskProjectOption> Projects { get; set; } = new();

        // Các chỉ số được TÍNH từ danh sách công việc (không đặt cứng), nên luôn khớp với dữ liệu.
        public int TotalTasks => Tasks.Count;
        public int TodoTasksCount => Tasks.Count(t => t.Status == "todo");
        public int InProgressTasksCount => Tasks.Count(t => t.Status == "in_progress");
        public int ReviewTasksCount => Tasks.Count(t => t.Status == "review");
        public int CompletedTasksCount => Tasks.Count(t => t.Status == "done");
        public int OverdueTasksCount => Tasks.Count(t => t.IsOverdue);

        // Số lượng theo phạm vi (các tab)
        public int PersonalTasksCount => Tasks.Count(t => !t.IsProjectTask);
        public int AssignedToMeCount => Tasks.Count(t => t.IsAssignedToMe);
        public int ProjectTasksCount => Tasks.Count(t => t.IsProjectTask);
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

        // ----- Liên kết với Quản lý dự án (tasks.project_id) -----
        /// <summary>Trống nghĩa là việc cá nhân.</summary>
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        // ----- Liên kết với Quản lý học tập (tasks.course_id) -----
        public string CourseCode { get; set; } = string.Empty;

        // ----- Người thực hiện / người giao (tasks.assigned_to, tasks.user_id) -----
        /// <summary>Tên người thực hiện. "Bạn" nếu là chính người đang đăng nhập.</summary>
        public string AssignedTo { get; set; } = "Bạn";

        /// <summary>Tên người đã giao việc này cho mình. Để trống nếu tự tạo.</summary>
        public string AssignedBy { get; set; } = string.Empty;

        public int? EstimatedMinutes { get; set; }
        public int SubtaskTotal { get; set; }
        public int SubtaskDone { get; set; }
        public List<TaskTagItem> Tags { get; set; } = new();
        public List<TaskCommentItem> Comments { get; set; } = new();

        public int CommentCount => Comments.Count;
        public bool IsProjectTask => ProjectId.HasValue;
        public bool IsAssignedToMe => !string.IsNullOrEmpty(AssignedBy);
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

    /// <summary>Một bình luận (từ bảng task_comments).</summary>
    public class TaskCommentItem
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Một dự án để chọn (từ bảng projects) kèm thành viên (từ bảng project_members).</summary>
    public class TaskProjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TaskMemberOption> Members { get; set; } = new();
    }

    public class TaskMemberOption
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>manager / member / viewer. Chỉ manager và member mới được nhận việc.</summary>
        public string Role { get; set; } = "member";
    }
}