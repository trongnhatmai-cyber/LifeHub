using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.ProjectManagement
{
    /// <summary>Các trạng thái dự án dùng chung, tránh gõ chuỗi tay nhiều nơi.</summary>
    public static class ProjectStatuses
    {
        public const string Running = "Đang chạy";
        public const string Planning = "Lên kế hoạch";
        public const string Paused = "Tạm dừng";
        public const string Completed = "Hoàn thành";
        public static readonly string[] All = { Running, Planning, Paused, Completed };
    }

    /// <summary>Các hàm định dạng dùng chung cho View (không chứa nghiệp vụ).</summary>
    public static class ProjectDisplay
    {
        public static string StatusKey(string status) => status switch
        {
            ProjectStatuses.Completed => "completed",
            ProjectStatuses.Planning => "planning",
            ProjectStatuses.Paused => "paused",
            _ => "running"
        };

        public static string DueText(DateTime dueDate, bool isCompleted)
        {
            if (isCompleted) return "Đã hoàn thành";
            var days = (dueDate.Date - DateTime.Today).Days;
            if (days < 0) return $"Quá hạn {-days} ngày";
            if (days == 0) return "Hạn hôm nay";
            return $"Còn {days} ngày";
        }

        public static string TimeAgo(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            return time.ToString("dd/MM HH:mm");
        }
    }

    // =====================================================================
    // TRANG DANH SÁCH DỰ ÁN
    // =====================================================================
    public class ProjectViewModel
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalTeamMembers { get; set; }

        public List<ProjectCardItem> Projects { get; set; } = new();
        public List<ProjectMilestoneItem> RecentMilestones { get; set; } = new();
        public List<ProjectFilterOption> StatusFilters { get; set; } = new();
    }

    public class ProjectCardItem
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = "Manager"; // Manager, Member, Viewer
        public int MemberCount { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = ProjectStatuses.Running;
        public DateTime DueDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public List<string> MemberNames { get; set; } = new();

        public string StatusKey => ProjectDisplay.StatusKey(Status);
        public string RoleKey => Role.ToLowerInvariant();
        public int DaysLeft => (DueDate.Date - DateTime.Today).Days;
        public bool IsOverdue => StatusKey != "completed" && DaysLeft < 0;
        public string DueText => ProjectDisplay.DueText(DueDate, StatusKey == "completed");
    }

    public class ProjectMilestoneItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanToggle { get; set; }

        public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;
    }

    public class ProjectFilterOption
    {
        public string Key { get; set; } = "all"; // all, running, planning, paused, completed
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // =====================================================================
    // TRANG CHI TIẾT DỰ ÁN (KANBAN)
    // =====================================================================
    public class ProjectDetailViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = ProjectStatuses.Running;
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int ProgressPercent { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }

        public string CurrentRole { get; set; } = "Viewer";
        public bool CanManage { get; set; } // Manager
        public bool CanEdit { get; set; }   // Manager + Member

        public List<BoardColumnItem> Columns { get; set; } = new();
        public List<ProjectMemberItem> Members { get; set; } = new();
        public List<ProjectInvitationItem> Invitations { get; set; } = new();
        public List<ProjectMilestoneItem> Milestones { get; set; } = new();
        public List<ProjectActivityItem> Activities { get; set; } = new();

        public string StatusKey => ProjectDisplay.StatusKey(Status);
        public string DueText => ProjectDisplay.DueText(DueDate, StatusKey == "completed");
    }

    public class BoardColumnItem
    {
        public string Key { get; set; } = string.Empty; // todo, in_progress, review, done
        public string Title { get; set; } = string.Empty;
        public List<TaskCardItem> Tasks { get; set; } = new();
    }

    public class TaskCardItem
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; // VD: LIFE-12
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StatusKey { get; set; } = "todo";
        public string StatusLabel { get; set; } = string.Empty;
        public string PriorityKey { get; set; } = "medium";
        public string PriorityLabel { get; set; } = string.Empty;
        public int? AssigneeId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public string AssigneeInitial { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public List<SubTaskItem> SubTasks { get; set; } = new();

        public int SubTaskDone => SubTasks.Count(s => s.IsDone);
    }

    public class SubTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }

    public class ProjectMemberItem
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public int OpenTaskCount { get; set; }
        public bool IsCurrentUser { get; set; }

        public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpperInvariant();
    }

    public class ProjectInvitationItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public string InvitedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string TimeText => ProjectDisplay.TimeAgo(CreatedAt);
    }

    public class ProjectActivityItem
    {
        public string ActorName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string TimeText => ProjectDisplay.TimeAgo(CreatedAt);
    }
}