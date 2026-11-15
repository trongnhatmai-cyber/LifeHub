using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.ProjectManagement
{
    /// <summary>Các trạng thái dự án dùng chung, tránh gõ chuỗi tay nhiều nơi.</summary>
    public static class ProjectStatuses
    {
        public const string Running = "Đang chạy";
        public const string Planning = "Lên kế hoạch";
        public const string Paused = "Tạm dừng";
        public const string Completed = "Hoàn thành";
    }

    public class ProjectViewModel
    {
        // Các số tổng quan do lớp cung cấp dữ liệu tính từ Projects (không hardcode)
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
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = "Manager"; // Manager, Member, Viewer
        public int MemberCount { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = ProjectStatuses.Running; // Đang chạy, Lên kế hoạch, Tạm dừng, Hoàn thành
        public DateTime DueDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }

        // ----- Thuộc tính mới -----
        public List<string> MemberNames { get; set; } = new();

        // ----- Thuộc tính phục vụ hiển thị (chỉ đọc) -----
        public string StatusKey => Status switch
        {
            ProjectStatuses.Completed => "completed",
            ProjectStatuses.Planning => "planning",
            ProjectStatuses.Paused => "paused",
            _ => "running"
        };

        public string RoleKey => Role.ToLowerInvariant();

        public int DaysLeft => (DueDate.Date - DateTime.Today).Days;

        public bool IsOverdue => StatusKey != "completed" && DaysLeft < 0;

        public string DueText
        {
            get
            {
                if (StatusKey == "completed") return "Đã hoàn thành";
                if (DaysLeft < 0) return $"Quá hạn {-DaysLeft} ngày";
                if (DaysLeft == 0) return "Hạn hôm nay";
                return $"Còn {DaysLeft} ngày";
            }
        }
    }

    public class ProjectMilestoneItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }

        public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;
    }

    public class ProjectFilterOption
    {
        public string Key { get; set; } = "all"; // all, running, planning, paused, completed
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}