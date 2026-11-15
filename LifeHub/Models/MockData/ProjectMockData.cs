using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.ProjectManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho phân hệ Dự án. Số việc và tiến độ của mỗi dự án đều được TÍNH từ danh sách
    /// nhiệm vụ, nên trang danh sách và trang chi tiết luôn khớp nhau (đúng như back-end sẽ tính sau này).
    /// Sau này thay bằng dữ liệu thật (projects, project_members, tasks, project_milestones) mà không phải sửa View.
    /// </summary>
    public static class ProjectMockData
    {
        private const string CurrentUser = "An";

        private record SubSeed(string Title, bool Done);
        private record TaskSeed(string Title, string Status, string Priority, int? DueDays, string? Assignee,
            string Description = "", SubSeed[]? Subs = null);
        private record ProjectSeed(int Id, string Key, string Name, string Description, string Role, string Status,
            int StartDays, int DueDays, string[] Members, TaskSeed[] Tasks);
        private record MilestoneSeed(int Id, int ProjectId, string Title, int DueDays, bool Done);

        private static readonly Dictionary<string, (int Id, string Email)> Users = new()
        {
            ["An"] = (1, "an@example.com"),
            ["Bình"] = (2, "binh@example.com"),
            ["Chi"] = (3, "chi@example.com"),
            ["Dũng"] = (4, "dung@example.com"),
            ["Hạnh"] = (5, "hanh@example.com"),
            ["Em"] = (6, "em@example.com"),
            ["Giang"] = (7, "giang@example.com")
        };

        private static readonly (string Key, string Title)[] Columns =
        {
            ("todo", "Cần làm"), ("in_progress", "Đang làm"), ("review", "Chờ duyệt"), ("done", "Hoàn thành")
        };

        private static readonly Dictionary<string, string> PriorityLabels = new()
        {
            ["low"] = "Thấp", ["medium"] = "Trung bình", ["high"] = "Cao", ["urgent"] = "Khẩn cấp"
        };

        // Lời mời đang chờ (chỉ hiện với Manager)
        private static readonly Dictionary<int, string> PendingInvites = new()
        {
            [1] = "duy@example.com",
            [4] = "tu@example.com"
        };

        private static readonly ProjectSeed[] Projects =
        {
            new(1, "LIFE", "LIFEHUB Platform",
                "Hệ thống quản lý cuộc sống toàn diện với kiến trúc Một User - Vạn Kết Nối.",
                "Manager", ProjectStatuses.Running, -60, 15,
                new[] { "An", "Bình", "Chi", "Dũng" },
                new TaskSeed[]
                {
                    new("Thiết kế ERD và Database", "done", "high", -10, "Bình"),
                    new("Layout, sidebar và theme", "done", "medium", -5, "An"),
                    new("Trang Dashboard", "done", "high", -3, "An"),
                    new("Phân hệ Học tập", "done", "medium", -1, "Chi"),
                    new("Phân hệ Dự án (Kanban)", "in_progress", "high", 4, "An",
                        "Trang danh sách, trang chi tiết và kéo thả nhiệm vụ.",
                        new SubSeed[] { new("Trang danh sách", true), new("Trang chi tiết", true), new("Kéo thả Kanban", false) }),
                    new("Phân hệ Tài chính", "in_progress", "medium", 8, "Dũng"),
                    new("Đăng nhập và phân quyền", "review", "high", 2, "Bình"),
                    new("Kiểm thử toàn hệ thống", "todo", "urgent", 12, "Chi"),
                    new("Viết báo cáo đồ án", "todo", "low", 14, null)
                }),

            new(2, "NET", "Website Nhóm NET108",
                "Ứng dụng web ASP.NET Core MVC tích hợp Entity Framework Core và SQL Server.",
                "Manager", ProjectStatuses.Running, -30, 25,
                new[] { "An", "Bình", "Chi" },
                new TaskSeed[]
                {
                    new("Cài đặt EF Core và SQL Server", "done", "high", -12, "An"),
                    new("Tạo Models và Migrations", "done", "medium", -8, "Bình"),
                    new("Làm trang đăng nhập", "in_progress", "medium", 3, "Chi",
                        "", new SubSeed[] { new("Giao diện", true), new("Xác thực", false) }),
                    new("Báo cáo tiến độ giai đoạn 1", "review", "medium", 6, "An"),
                    new("CRUD sản phẩm", "todo", "high", 10, "Bình"),
                    new("Slide thuyết trình", "todo", "low", 20, null)
                }),

            new(3, "UXP", "UI/UX Redesign Portfolio",
                "Thiết kế lại giao diện portfolio cá nhân với phong cách tối giản và pastel.",
                "Member", ProjectStatuses.Completed, -45, -5,
                new[] { "Hạnh", "An" },
                new TaskSeed[]
                {
                    new("Wireframe các trang chính", "done", "medium", -30, "Hạnh"),
                    new("Chọn bảng màu pastel", "done", "low", -25, "An"),
                    new("Thiết kế trang chủ", "done", "high", -15, "Hạnh"),
                    new("Kiểm tra responsive", "done", "medium", -8, "An")
                }),

            new(4, "IELTS", "Kế hoạch ôn thi IELTS 7.0",
                "Lộ trình ôn luyện 4 kỹ năng, làm đề theo tuần và theo dõi band điểm.",
                "Manager", ProjectStatuses.Planning, -2, 60,
                new[] { "An" },
                new TaskSeed[]
                {
                    new("Làm bài test đầu vào", "todo", "high", 3, "An"),
                    new("Chốt lộ trình 12 tuần", "todo", "medium", 10, "An"),
                    new("Mua tài liệu Cambridge", "todo", "low", 7, null),
                    new("Đăng ký lịch thi", "todo", "medium", 20, "An")
                }),

            new(5, "DB", "Đồ án Cơ sở dữ liệu",
                "Thiết kế CSDL quản lý thư viện, viết truy vấn và báo cáo cuối kỳ.",
                "Member", ProjectStatuses.Paused, -40, -3,
                new[] { "Em", "An", "Giang" },
                new TaskSeed[]
                {
                    new("Phân tích yêu cầu thư viện", "done", "medium", -30, "Em"),
                    new("Vẽ sơ đồ ERD", "done", "high", -20, "An"),
                    new("Chuẩn hóa 3NF", "in_progress", "high", -1, "An"),
                    new("Viết 20 truy vấn SQL", "todo", "medium", -2, "Giang"),
                    new("Báo cáo cuối kỳ", "todo", "high", 5, "Em")
                })
        };

        private static readonly MilestoneSeed[] MilestoneSeeds =
        {
            new(1, 1, "Hoàn thành thiết kế ERD & Database", -2, true),
            new(2, 1, "Triển khai toàn bộ Front-End Views", 3, false),
            new(3, 2, "Báo cáo tiến độ giai đoạn 1", 7, false),
            new(4, 4, "Chốt lộ trình ôn thi 12 tuần", 10, false),
            new(5, 5, "Nộp bản thiết kế ERD", -10, true),
            new(6, 3, "Bàn giao portfolio", -5, true)
        };

        // =====================================================================
        // TRANG DANH SÁCH
        // =====================================================================
        public static ProjectViewModel BuildIndex()
        {
            var today = DateTime.Today;
            var projects = Projects.Select(p => ToCard(p, today)).ToList();

            var milestones = MilestoneSeeds
                .Select(m => ToMilestone(m, projects.First(p => p.Id == m.ProjectId), today))
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
                    new() { Key = "all",       Label = "Tất cả",                  Count = projects.Count },
                    new() { Key = "running",   Label = ProjectStatuses.Running,   Count = projects.Count(p => p.StatusKey == "running") },
                    new() { Key = "planning",  Label = ProjectStatuses.Planning,  Count = projects.Count(p => p.StatusKey == "planning") },
                    new() { Key = "paused",    Label = ProjectStatuses.Paused,    Count = projects.Count(p => p.StatusKey == "paused") },
                    new() { Key = "completed", Label = ProjectStatuses.Completed, Count = projects.Count(p => p.StatusKey == "completed") }
                }
            };
        }

        // =====================================================================
        // TRANG CHI TIẾT (KANBAN). Trả về null nếu không có dự án này.
        // =====================================================================
        public static ProjectDetailViewModel? BuildDetail(int id)
        {
            var today = DateTime.Today;
            var seed = Projects.FirstOrDefault(p => p.Id == id);
            if (seed == null) return null;

            var card = ToCard(seed, today);
            var canManage = seed.Role == "Manager";
            var canEdit = seed.Role is "Manager" or "Member";

            // Nhiệm vụ
            var tasks = seed.Tasks.Select((t, i) =>
            {
                var taskId = seed.Id * 100 + i + 1;
                var due = t.DueDays.HasValue ? today.AddDays(t.DueDays.Value) : (DateTime?)null;
                var hasAssignee = !string.IsNullOrEmpty(t.Assignee) && Users.ContainsKey(t.Assignee);

                return new TaskCardItem
                {
                    Id = taskId,
                    Key = $"{seed.Key}-{i + 1}",
                    Title = t.Title,
                    Description = t.Description,
                    StatusKey = t.Status,
                    StatusLabel = Columns.First(c => c.Key == t.Status).Title,
                    PriorityKey = t.Priority,
                    PriorityLabel = PriorityLabels[t.Priority],
                    AssigneeId = hasAssignee ? Users[t.Assignee!].Id : (int?)null,
                    AssigneeName = hasAssignee ? t.Assignee! : string.Empty,
                    AssigneeInitial = hasAssignee ? t.Assignee!.Substring(0, 1).ToUpperInvariant() : string.Empty,
                    DueDate = due,
                    IsOverdue = due.HasValue && due.Value.Date < today && t.Status != "done",
                    SubTasks = (t.Subs ?? Array.Empty<SubSeed>())
                        .Select((s, j) => new SubTaskItem { Id = taskId * 10 + j + 1, Title = s.Title, IsDone = s.Done })
                        .ToList()
                };
            }).ToList();

            // Thành viên: bạn giữ vai trò của dự án; nếu bạn không phải Manager thì người đầu tiên còn lại là Manager
            var others = seed.Members.Where(n => n != CurrentUser).ToList();
            var members = seed.Members.Select(n =>
            {
                var role = n == CurrentUser
                    ? seed.Role
                    : (seed.Role != "Manager" && n == others.First() ? "Manager" : "Member");

                return new ProjectMemberItem
                {
                    UserId = Users[n].Id,
                    Name = n,
                    Email = Users[n].Email,
                    Role = role,
                    OpenTaskCount = seed.Tasks.Count(t => t.Assignee == n && t.Status != "done"),
                    IsCurrentUser = n == CurrentUser
                };
            }).ToList();

            var invitations = new List<ProjectInvitationItem>();
            if (canManage && PendingInvites.TryGetValue(seed.Id, out var inviteEmail))
            {
                invitations.Add(new ProjectInvitationItem
                {
                    Id = seed.Id * 100 + 1,
                    Email = inviteEmail,
                    Role = "Member",
                    InvitedByName = CurrentUser,
                    CreatedAt = DateTime.Now.AddHours(-3)
                });
            }

            var milestones = MilestoneSeeds
                .Where(m => m.ProjectId == seed.Id)
                .Select(m => ToMilestone(m, card, today))
                .OrderBy(m => m.DueDate)
                .ToList();

            // Hoạt động gần đây: suy ra từ nhiệm vụ đã xong
            var activities = tasks.Where(t => t.StatusKey == "done").Take(2)
                .Select((t, i) => new ProjectActivityItem
                {
                    ActorName = string.IsNullOrEmpty(t.AssigneeName) ? CurrentUser : t.AssigneeName,
                    Text = $"đã hoàn thành nhiệm vụ {t.Key}",
                    CreatedAt = DateTime.Now.AddHours(-(i + 1) * 5)
                })
                .ToList();
            activities.Add(new ProjectActivityItem
            {
                ActorName = CurrentUser,
                Text = "đã tạo dự án",
                CreatedAt = today.AddDays(seed.StartDays).AddHours(9)
            });

            return new ProjectDetailViewModel
            {
                Id = seed.Id,
                Key = seed.Key,
                Name = seed.Name,
                Description = seed.Description,
                Status = seed.Status,
                StartDate = today.AddDays(seed.StartDays),
                DueDate = today.AddDays(seed.DueDays),
                ProgressPercent = card.ProgressPercent,
                TotalTasks = card.TotalTasks,
                CompletedTasks = card.CompletedTasks,
                CurrentRole = seed.Role,
                CanManage = canManage,
                CanEdit = canEdit,
                Columns = Columns.Select(c => new BoardColumnItem
                {
                    Key = c.Key,
                    Title = c.Title,
                    Tasks = tasks.Where(t => t.StatusKey == c.Key).ToList()
                }).ToList(),
                Members = members,
                Invitations = invitations,
                Milestones = milestones,
                Activities = activities.OrderByDescending(a => a.CreatedAt).ToList()
            };
        }

        // =====================================================================
        // HÀM DÙNG CHUNG
        // =====================================================================
        private static ProjectCardItem ToCard(ProjectSeed s, DateTime today)
        {
            var total = s.Tasks.Length;
            var done = s.Tasks.Count(t => t.Status == "done");

            return new ProjectCardItem
            {
                Id = s.Id,
                Key = s.Key,
                Name = s.Name,
                Description = s.Description,
                Role = s.Role,
                Status = s.Status,
                DueDate = today.AddDays(s.DueDays),
                TotalTasks = total,
                CompletedTasks = done,
                // Tiến độ = số việc đã xong / tổng số việc
                ProgressPercent = total == 0 ? 0 : (int)Math.Round(done * 100.0 / total),
                MemberNames = s.Members.ToList(),
                MemberCount = s.Members.Length
            };
        }

        private static ProjectMilestoneItem ToMilestone(MilestoneSeed s, ProjectCardItem project, DateTime today)
        {
            return new ProjectMilestoneItem
            {
                Id = s.Id,
                ProjectId = project.Id,
                ProjectName = project.Name, // lấy từ dự án để luôn khớp tên
                Title = s.Title,
                DueDate = today.AddDays(s.DueDays),
                IsCompleted = s.Done,
                CanToggle = project.Role == "Manager" // chỉ Manager được tick cột mốc
            };
        }
    }
}