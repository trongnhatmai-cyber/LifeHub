using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.Domain;
using LifeHub.Models.ViewModels.ProjectManagement;
using static LifeHub.Models.Domain.ProjectRoles;
using static LifeHub.Models.Domain.TaskPriorities;
using static LifeHub.Models.Domain.TaskStatuses;

namespace LifeHub.Models.MockData
{
    public class ProjectSeed
    {
        public List<UserEntity> Users { get; } = new();
        public List<ProjectEntity> Projects { get; } = new();
        public List<ProjectMemberEntity> Members { get; } = new();
        public List<ProjectInvitationEntity> Invitations { get; } = new();
        public List<ProjectMilestoneEntity> Milestones { get; } = new();
        public List<ProjectTaskEntity> Tasks { get; } = new();
        public List<ProjectActivityEntity> Activities { get; } = new();
    }

    /// <summary>Dữ liệu mẫu ban đầu cho phân hệ Dự án. Sau này thay bằng dữ liệu thật.</summary>
    public static class ProjectMockData
    {
        public static ProjectSeed CreateSeed()
        {
            var today = DateTime.Today;
            var s = new ProjectSeed();

            // ----- Người dùng (An = người đang đăng nhập giả định) -----
            string[] names = { "An", "Bình", "Chi", "Dũng", "Hạnh", "Em", "Giang" };
            string[] mails = { "an", "binh", "chi", "dung", "hanh", "em", "giang" };
            for (var i = 0; i < names.Length; i++)
                s.Users.Add(new UserEntity { Id = i + 1, Name = names[i], Email = mails[i] + "@lifehub.local" });

            // ----- Dự án -----
            AddProject(s, 1, "LIFE", "LIFEHUB Platform", "Hệ thống quản lý cuộc sống toàn diện với kiến trúc Một User - Vạn Kết Nối.", today.AddDays(-45), today.AddDays(15), ProjectStatuses.Running, 1);
            AddProject(s, 2, "NET", "Website Nhóm NET108", "Ứng dụng web ASP.NET Core MVC tích hợp Entity Framework Core và SQL Server.", today.AddDays(-30), today.AddDays(25), ProjectStatuses.Running, 1);
            AddProject(s, 3, "PORT", "UI/UX Redesign Portfolio", "Thiết kế lại giao diện portfolio cá nhân với phong cách tối giản và pastel.", today.AddDays(-40), today.AddDays(-5), ProjectStatuses.Completed, 5);
            AddProject(s, 4, "IELTS", "Kế hoạch ôn thi IELTS 7.0", "Lộ trình ôn luyện 4 kỹ năng, làm đề theo tuần và theo dõi band điểm.", today, today.AddDays(60), ProjectStatuses.Planning, 1);
            AddProject(s, 5, "CSDL", "Đồ án Cơ sở dữ liệu", "Thiết kế CSDL quản lý thư viện, viết truy vấn và báo cáo cuối kỳ.", today.AddDays(-35), today.AddDays(-3), ProjectStatuses.Paused, 6);

            // ----- Thành viên theo dự án (project_members) -----
            AddMember(s, 1, 1, Manager); AddMember(s, 1, 2, Member); AddMember(s, 1, 3, Member); AddMember(s, 1, 4, Viewer);
            AddMember(s, 2, 1, Manager); AddMember(s, 2, 2, Member); AddMember(s, 2, 3, Member);
            AddMember(s, 3, 5, Manager); AddMember(s, 3, 1, Member);
            AddMember(s, 4, 1, Manager);
            AddMember(s, 5, 6, Manager); AddMember(s, 5, 1, Member); AddMember(s, 5, 7, Member);

            // ----- Nhiệm vụ: dự án 1 -----
            AddTask(s, 1, "Thiết kế ERD & Database", Done, High, 1, -10);
            AddTask(s, 1, "Dựng Layout và Sidebar", Done, Medium, 2, -7);
            AddTask(s, 1, "Trang Tổng quan", Done, Medium, 3, -3);
            AddTask(s, 1, "Phân hệ Học tập", Review, High, 2, 1);
            AddTask(s, 1, "Phân hệ Tài chính", InProgress, High, 3, 4);
            var t6 = AddTask(s, 1, "Phân hệ Quản lý dự án", InProgress, Urgent, 1, 3);
            AddTask(s, 1, "Phân hệ Thói quen", Todo, Medium, null, 9);
            AddTask(s, 1, "Viết báo cáo cuối kỳ", Todo, Low, null, 14);
            AddTask(s, 1, "Danh sách dự án", Done, Medium, 1, 0, t6.Id);
            AddTask(s, 1, "Trang chi tiết dự án", Todo, Medium, 1, 0, t6.Id);
            AddTask(s, 1, "Bảng Kanban", Todo, Medium, 1, 0, t6.Id);
            AddTask(s, 1, "Mời thành viên", Todo, Medium, 1, 0, t6.Id);

            // ----- Nhiệm vụ: dự án 2 -----
            AddTask(s, 2, "Khởi tạo project ASP.NET Core MVC", Done, High, 1, -20);
            AddTask(s, 2, "Thiết kế CSDL SQL Server", Done, High, 2, -15);
            AddTask(s, 2, "Cấu hình Entity Framework Core", Done, Medium, 2, -10);
            AddTask(s, 2, "Làm trang đăng nhập", InProgress, Medium, 3, 5);
            AddTask(s, 2, "Làm chức năng CRUD sản phẩm", Todo, Medium, 2, 12);
            AddTask(s, 2, "Kiểm thử và viết báo cáo", Todo, Low, null, 22);

            // ----- Nhiệm vụ: dự án 3 (đã xong) -----
            AddTask(s, 3, "Phác thảo wireframe", Done, Medium, 5, -35);
            AddTask(s, 3, "Chọn bảng màu pastel", Done, Low, 1, -30);
            AddTask(s, 3, "Thiết kế trang chủ", Done, High, 5, -15);
            AddTask(s, 3, "Xuất file bàn giao", Done, Medium, 1, -6);

            // ----- Nhiệm vụ: dự án 4 -----
            AddTask(s, 4, "Làm bài test đầu vào", Todo, High, 1, 7);
            AddTask(s, 4, "Chọn giáo trình", Todo, Medium, 1, 10);
            AddTask(s, 4, "Lên lịch ôn 12 tuần", Todo, Medium, 1, 14);

            // ----- Nhiệm vụ: dự án 5 (tạm dừng) -----
            AddTask(s, 5, "Vẽ ERD", Done, High, 6, -25);
            AddTask(s, 5, "Chuẩn hóa về 3NF", Done, Medium, 7, -18);
            AddTask(s, 5, "Viết script tạo bảng", InProgress, Medium, 1, -4);
            AddTask(s, 5, "Viết truy vấn báo cáo", Todo, Medium, 7, 2);
            AddTask(s, 5, "Làm slide thuyết trình", Todo, Low, 6, 5);

            // ----- Cột mốc -----
            AddMilestone(s, 1, "Hoàn thành thiết kế ERD & Database", today.AddDays(-2), true);
            AddMilestone(s, 1, "Triển khai toàn bộ Front-End Views", today.AddDays(3), false);
            AddMilestone(s, 2, "Báo cáo tiến độ giai đoạn 1", today.AddDays(7), false);
            AddMilestone(s, 4, "Chốt lộ trình ôn thi 12 tuần", today.AddDays(10), false);

            // ----- Lời mời mẫu -----
            s.Invitations.Add(new ProjectInvitationEntity
            {
                Id = 1, ProjectId = 1, Email = "khoa@lifehub.local", Role = Member,
                InvitedBy = 1, Status = InvitationStatuses.Pending, CreatedAt = DateTime.Now.AddHours(-5)
            });

            // ----- Nhật ký hoạt động mẫu -----
            AddActivity(s, 1, 1, "đã tạo dự án", 72);
            AddActivity(s, 1, 2, "đã chuyển LIFE-4 sang \"Chờ duyệt\"", 20);
            AddActivity(s, 1, 3, "đã chuyển LIFE-5 sang \"Đang làm\"", 6);
            AddActivity(s, 1, 1, "đã mời khoa@lifehub.local tham gia với vai trò Member", 5);

            return s;
        }

        private static void AddProject(ProjectSeed s, int id, string key, string name, string description,
            DateTime start, DateTime due, string status, int createdBy)
        {
            s.Projects.Add(new ProjectEntity
            {
                Id = id, Key = key, Name = name, Description = description,
                StartDate = start, DueDate = due, Status = status, CreatedBy = createdBy
            });
        }

        private static void AddMember(ProjectSeed s, int projectId, int userId, string role)
        {
            s.Members.Add(new ProjectMemberEntity
            {
                ProjectId = projectId, UserId = userId, Role = role, JoinedAt = DateTime.Now.AddDays(-30)
            });
        }

        private static ProjectTaskEntity AddTask(ProjectSeed s, int projectId, string title, string status,
            string priority, int? assigneeId, int dueOffsetDays, int? parentTaskId = null)
        {
            var project = s.Projects.First(p => p.Id == projectId);
            var task = new ProjectTaskEntity
            {
                Id = s.Tasks.Count + 1,
                ProjectId = projectId,
                Number = ++project.TaskCounter,
                Title = title,
                Status = status,
                Priority = priority,
                AssigneeId = assigneeId,
                ParentTaskId = parentTaskId,
                DueDate = parentTaskId == null ? DateTime.Today.AddDays(dueOffsetDays) : null,
                SortOrder = s.Tasks.Count + 1,
                CreatedBy = 1,
                CreatedAt = DateTime.Now.AddDays(-30)
            };
            s.Tasks.Add(task);
            return task;
        }

        private static void AddMilestone(ProjectSeed s, int projectId, string title, DateTime due, bool done)
        {
            s.Milestones.Add(new ProjectMilestoneEntity
            {
                Id = s.Milestones.Count + 1, ProjectId = projectId, Title = title, DueDate = due, IsCompleted = done
            });
        }

        private static void AddActivity(ProjectSeed s, int projectId, int userId, string text, int hoursAgo)
        {
            s.Activities.Add(new ProjectActivityEntity
            {
                Id = s.Activities.Count + 1, ProjectId = projectId, UserId = userId,
                Text = text, CreatedAt = DateTime.Now.AddHours(-hoursAgo)
            });
        }
    }
}