using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.ProjectManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho phân hệ Dự án. Sau này thay bằng dữ liệu thật
    /// (projects, project_members, project_milestones) mà không phải sửa View.
    /// </summary>
    public static class ProjectMockData
    {
        public static ProjectViewModel BuildIndex()
        {
            var today = DateTime.Today;

            var projects = new List<ProjectCardItem>
            {
                CreateProject(1, "LIFEHUB Platform",
                    "Hệ thống quản lý cuộc sống toàn diện với kiến trúc Một User - Vạn Kết Nối.",
                    "Manager", ProjectStatuses.Running, today.AddDays(15), 24, 18,
                    "An", "Bình", "Chi", "Dũng"),

                CreateProject(2, "Website Nhóm NET108",
                    "Ứng dụng web ASP.NET Core MVC tích hợp Entity Framework Core và SQL Server.",
                    "Manager", ProjectStatuses.Running, today.AddDays(25), 16, 8,
                    "An", "Bình", "Chi"),

                CreateProject(3, "UI/UX Redesign Portfolio",
                    "Thiết kế lại giao diện portfolio cá nhân với phong cách tối giản và pastel.",
                    "Member", ProjectStatuses.Completed, today.AddDays(-5), 10, 10,
                    "Hạnh", "An"),

                CreateProject(4, "Kế hoạch ôn thi IELTS 7.0",
                    "Lộ trình ôn luyện 4 kỹ năng, làm đề theo tuần và theo dõi band điểm.",
                    "Manager", ProjectStatuses.Planning, today.AddDays(60), 12, 0,
                    "An"),

                CreateProject(5, "Đồ án Cơ sở dữ liệu",
                    "Thiết kế CSDL quản lý thư viện, viết truy vấn và báo cáo cuối kỳ.",
                    "Member", ProjectStatuses.Paused, today.AddDays(-3), 20, 6,
                    "Em", "An", "Giang")
            };

            var milestones = new List<ProjectMilestoneItem>
            {
                CreateMilestone(1, projects[0], "Hoàn thành thiết kế ERD & Database", today.AddDays(-2), true),
                CreateMilestone(2, projects[0], "Triển khai toàn bộ Front-End Views", today.AddDays(3), false),
                CreateMilestone(3, projects[1], "Báo cáo tiến độ giai đoạn 1", today.AddDays(7), false),
                CreateMilestone(4, projects[3], "Chốt lộ trình ôn thi 12 tuần", today.AddDays(10), false)
            }
            .OrderBy(m => m.DueDate)
            .ToList();

            return new ProjectViewModel
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.StatusKey == "running"),
                CompletedProjects = projects.Count(p => p.StatusKey == "completed"),
                TotalTeamMembers = projects.SelectMany(p => p.MemberNames).Distinct().Count(),
                Projects = projects,
                RecentMilestones = milestones,
                StatusFilters = new List<ProjectFilterOption>
                {
                    new() { Key = "all",       Label = "Tất cả",                Count = projects.Count },
                    new() { Key = "running",   Label = ProjectStatuses.Running,   Count = projects.Count(p => p.StatusKey == "running") },
                    new() { Key = "planning",  Label = ProjectStatuses.Planning,  Count = projects.Count(p => p.StatusKey == "planning") },
                    new() { Key = "paused",    Label = ProjectStatuses.Paused,    Count = projects.Count(p => p.StatusKey == "paused") },
                    new() { Key = "completed", Label = ProjectStatuses.Completed, Count = projects.Count(p => p.StatusKey == "completed") }
                }
            };
        }

        private static ProjectCardItem CreateProject(
            int id, string name, string description, string role, string status,
            DateTime dueDate, int totalTasks, int completedTasks, params string[] members)
        {
            return new ProjectCardItem
            {
                Id = id,
                Name = name,
                Description = description,
                Role = role,
                Status = status,
                DueDate = dueDate,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                // Tiến độ = số việc đã xong / tổng số việc
                ProgressPercent = totalTasks == 0 ? 0 : (int)Math.Round(completedTasks * 100.0 / totalTasks),
                MemberNames = members.ToList(),
                MemberCount = members.Length
            };
        }

        private static ProjectMilestoneItem CreateMilestone(
            int id, ProjectCardItem project, string title, DateTime dueDate, bool isCompleted)
        {
            return new ProjectMilestoneItem
            {
                Id = id,
                ProjectId = project.Id,
                ProjectName = project.Name, // lấy từ dự án để luôn khớp tên
                Title = title,
                DueDate = dueDate,
                IsCompleted = isCompleted
            };
        }
    }
}