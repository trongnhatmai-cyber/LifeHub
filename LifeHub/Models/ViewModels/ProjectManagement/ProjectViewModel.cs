using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.ProjectManagement
{
    public class ProjectViewModel
    {
        public int TotalProjects { get; set; } = 3;
        public int ActiveProjects { get; set; } = 2;
        public int CompletedProjects { get; set; } = 1;
        public int TotalTeamMembers { get; set; } = 7;

        public List<ProjectCardItem> Projects { get; set; } = new();
        public List<ProjectMilestoneItem> RecentMilestones { get; set; } = new();
    }

    public class ProjectCardItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = "Manager"; // Manager, Member, Viewer
        public int MemberCount { get; set; } = 3;
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = "Đang chạy"; // Đang chạy, Lên kế hoạch, Hoàn thành
        public DateTime DueDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }

    public class ProjectMilestoneItem
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}
