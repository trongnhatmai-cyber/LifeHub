using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.ProjectManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu CHỐT cho phân hệ Dự án (12 dự án). Số việc, tiến độ, lịch sử, "nhiệm vụ gần đây"
    /// và thứ tự "dự án gần đây" đều được TÍNH từ cùng một danh sách nhiệm vụ nên các trang luôn khớp nhau.
    /// Sau này thay bằng dữ liệu thật (projects, project_members, tasks, task_comments, project_activities).
    /// </summary>
    public static class ProjectMockData
    {
        private const string CurrentUser = "An";
        private const int RecentProjectCount = 6;

        private record SubSeed(string Title, bool Done);
        private record CommentSeed(string Author, string Text, int HoursAgo, bool IsRework = false);
        private record TaskSeed(string Title, string Status, string Priority, int? DueDays, string? Assignee,
            string Description = "", SubSeed[]? Subs = null, string? Reporter = null, int AssignedHoursAgo = 72,
            CommentSeed[]? Comments = null);
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
            [4] = "tu@example.com",
            [7] = "khoa@example.com"
        };

        private static readonly ProjectSeed[] Projects =
        {
            // ---------------------------------------------------------------- 1
            new(1, "LIFE", "LIFEHUB Platform",
                "Hệ thống quản lý cuộc sống toàn diện với kiến trúc Một User - Vạn Kết Nối.",
                "Manager", ProjectStatuses.Running, -60, 15,
                new[] { "An", "Bình", "Chi", "Dũng" },
                new TaskSeed[]
                {
                    new("Thiết kế ERD và Database", "done", "high", -10, "Bình", AssignedHoursAgo: 400),
                    new("Layout, sidebar và theme", "done", "medium", -5, "An", AssignedHoursAgo: 300),
                    new("Trang Dashboard", "done", "high", -3, "An", AssignedHoursAgo: 240),
                    new("Phân hệ Học tập", "done", "medium", -1, "Chi", AssignedHoursAgo: 200),
                    new("Phân hệ Dự án (Kanban)", "in_progress", "high", 4, "An",
                        "Trang danh sách, trang chi tiết, kéo thả nhiệm vụ và quy trình duyệt.",
                        new SubSeed[] { new("Trang danh sách", true), new("Trang chi tiết", true), new("Kéo thả Kanban", false) },
                        AssignedHoursAgo: 20),
                    new("Phân hệ Tài chính", "in_progress", "medium", 8, "Dũng",
                        "Ví, giao dịch, ngân sách và biểu đồ thu chi theo tháng.",
                        AssignedHoursAgo: 90,
                        Comments: new CommentSeed[]
                        {
                            new("Dũng", "Em đã làm xong phần ví và biểu đồ thu chi, nhờ anh xem giúp ạ.", 30),
                            new("An", "Chưa đạt: biểu đồ chưa lọc được theo tháng và chưa kiểm tra số tiền âm. Em sửa rồi gửi duyệt lại nhé.", 20, true)
                        }),
                    new("Đăng nhập và phân quyền", "review", "high", 2, "Bình",
                        "Đăng ký, đăng nhập và phân quyền Admin/User.",
                        AssignedHoursAgo: 40,
                        Comments: new CommentSeed[]
                        {
                            new("Bình", "Em đã làm xong đăng nhập và phân quyền Admin/User. Nhờ anh duyệt giúp em.", 3)
                        }),
                    new("Kiểm thử toàn hệ thống", "todo", "urgent", 12, "Chi", AssignedHoursAgo: 6),
                    new("Viết báo cáo đồ án", "todo", "low", 14, null, AssignedHoursAgo: 120)
                }),

            // ---------------------------------------------------------------- 2
            new(2, "NET", "Website Nhóm NET108",
                "Ứng dụng web ASP.NET Core MVC tích hợp Entity Framework Core và SQL Server.",
                "Manager", ProjectStatuses.Running, -30, 25,
                new[] { "An", "Bình", "Chi" },
                new TaskSeed[]
                {
                    new("Cài đặt EF Core và SQL Server", "done", "high", -12, "An", AssignedHoursAgo: 500),
                    new("Tạo Models và Migrations", "done", "medium", -8, "Bình", AssignedHoursAgo: 380),
                    new("Làm trang đăng nhập", "in_progress", "medium", 3, "Chi",
                        "Form đăng nhập, kiểm tra dữ liệu và xác thực người dùng.",
                        new SubSeed[] { new("Giao diện", true), new("Xác thực", false) },
                        AssignedHoursAgo: 30),
                    new("Báo cáo tiến độ giai đoạn 1", "review", "medium", 6, "An", AssignedHoursAgo: 12),
                    new("CRUD sản phẩm", "todo", "high", 10, "Bình", AssignedHoursAgo: 4),
                    new("Slide thuyết trình", "todo", "low", 20, null, AssignedHoursAgo: 100)
                }),

            // ---------------------------------------------------------------- 3
            new(3, "UXP", "UI/UX Redesign Portfolio",
                "Thiết kế lại giao diện portfolio cá nhân với phong cách tối giản và pastel.",
                "Member", ProjectStatuses.Completed, -45, -5,
                new[] { "Hạnh", "An" },
                new TaskSeed[]
                {
                    new("Wireframe các trang chính", "done", "medium", -30, "Hạnh", AssignedHoursAgo: 900),
                    new("Chọn bảng màu pastel", "done", "low", -25, "An", AssignedHoursAgo: 800),
                    new("Thiết kế trang chủ", "done", "high", -15, "Hạnh", AssignedHoursAgo: 600),
                    new("Kiểm tra responsive", "done", "medium", -8, "An", AssignedHoursAgo: 400)
                }),

            // ---------------------------------------------------------------- 4
            new(4, "IELTS", "Kế hoạch ôn thi IELTS 7.0",
                "Lộ trình ôn luyện 4 kỹ năng, làm đề theo tuần và theo dõi band điểm.",
                "Manager", ProjectStatuses.Planning, -2, 60,
                new[] { "An" },
                new TaskSeed[]
                {
                    new("Làm bài test đầu vào", "todo", "high", 3, "An", AssignedHoursAgo: 2),
                    new("Chốt lộ trình 12 tuần", "todo", "medium", 10, "An", AssignedHoursAgo: 28),
                    new("Mua tài liệu Cambridge", "todo", "low", 7, null, AssignedHoursAgo: 30),
                    new("Đăng ký lịch thi", "todo", "medium", 20, "An", AssignedHoursAgo: 46)
                }),

            // ---------------------------------------------------------------- 5
            new(5, "DB", "Đồ án Cơ sở dữ liệu",
                "Thiết kế CSDL quản lý thư viện, viết truy vấn và báo cáo cuối kỳ.",
                "Member", ProjectStatuses.Paused, -40, -3,
                new[] { "Em", "An", "Giang" },
                new TaskSeed[]
                {
                    new("Phân tích yêu cầu thư viện", "done", "medium", -30, "Em", AssignedHoursAgo: 700),
                    new("Vẽ sơ đồ ERD", "done", "high", -20, "An", AssignedHoursAgo: 500),
                    new("Chuẩn hóa 3NF", "in_progress", "high", -1, "An",
                        "Chuẩn hóa toàn bộ lược đồ về 3NF, kể cả bảng mượn trả.",
                        AssignedHoursAgo: 8,
                        Comments: new CommentSeed[]
                        {
                            new("Em", "Nhớ kiểm tra cả bảng mượn trả nhé, thầy hay hỏi phần này.", 7)
                        }),
                    new("Viết 20 truy vấn SQL", "todo", "medium", -2, "Giang", AssignedHoursAgo: 60),
                    new("Báo cáo cuối kỳ", "todo", "high", 5, "Em", AssignedHoursAgo: 64)
                }),

            // ---------------------------------------------------------------- 6
            new(6, "MKT", "Chiến dịch Marketing Đoàn trường",
                "Lên kế hoạch truyền thông, thiết kế ấn phẩm và theo dõi hiệu quả chiến dịch.",
                "Member", ProjectStatuses.Running, -20, 18,
                new[] { "Giang", "An", "Em", "Hạnh" },
                new TaskSeed[]
                {
                    new("Lập kế hoạch truyền thông", "done", "high", -10, "Giang", AssignedHoursAgo: 300),
                    new("Thiết kế poster và banner", "in_progress", "medium", 3, "Hạnh", AssignedHoursAgo: 30),
                    new("Viết nội dung bài đăng Fanpage", "in_progress", "medium", 5, "An",
                        "Viết 8 bài đăng cho 2 tuần đầu của chiến dịch.", AssignedHoursAgo: 16),
                    new("Liên hệ nhà tài trợ", "review", "high", 1, "Em", AssignedHoursAgo: 50),
                    new("Báo cáo hiệu quả chiến dịch", "todo", "low", 16, null, AssignedHoursAgo: 100)
                }),

            // ---------------------------------------------------------------- 7
            new(7, "APP", "Ứng dụng Quản lý chi tiêu",
                "Ứng dụng di động ghi chép thu chi, ngân sách và báo cáo theo tháng.",
                "Manager", ProjectStatuses.Running, -25, 30,
                new[] { "An", "Dũng", "Hạnh" },
                new TaskSeed[]
                {
                    new("Khảo sát nhu cầu người dùng", "done", "medium", -18, "Hạnh", AssignedHoursAgo: 450),
                    new("Thiết kế giao diện ví và giao dịch", "review", "high", 2, "Hạnh",
                        "Màn hình ví, danh sách giao dịch và form thêm giao dịch.",
                        AssignedHoursAgo: 26,
                        Comments: new CommentSeed[]
                        {
                            new("Hạnh", "Em đã hoàn thành bản thiết kế ví, nhờ anh xem giúp ạ.", 5)
                        }),
                    new("Xây dựng API giao dịch", "in_progress", "high", 9, "Dũng", AssignedHoursAgo: 10),
                    new("Biểu đồ thu chi theo tháng", "todo", "medium", 14, "Dũng", AssignedHoursAgo: 12),
                    new("Kiểm thử và phát hành bản beta", "todo", "urgent", 27, null, AssignedHoursAgo: 60)
                }),

            // ---------------------------------------------------------------- 8 (bạn chỉ là Viewer)
            new(8, "EVT", "Tổ chức sự kiện CLB Tin học",
                "Hội thảo định hướng nghề nghiệp dành cho sinh viên năm nhất.",
                "Viewer", ProjectStatuses.Running, -15, 12,
                new[] { "Chi", "Bình", "An" },
                new TaskSeed[]
                {
                    new("Chốt diễn giả và lịch trình", "done", "high", -8, "Chi", AssignedHoursAgo: 260),
                    new("Đặt hội trường", "done", "medium", -5, "Bình", AssignedHoursAgo: 220),
                    new("Thiết kế phông nền và standee", "in_progress", "medium", 4, "Bình", AssignedHoursAgo: 170),
                    new("Mở đăng ký tham dự", "todo", "high", 6, "Chi", AssignedHoursAgo: 160)
                }),

            // ---------------------------------------------------------------- 9
            new(9, "THES", "Khóa luận tốt nghiệp",
                "Nghiên cứu hệ thống gợi ý học liệu, viết báo cáo và chuẩn bị bảo vệ.",
                "Manager", ProjectStatuses.Running, -90, 45,
                new[] { "An", "Em" },
                new TaskSeed[]
                {
                    new("Tổng quan tài liệu", "done", "medium", -50, "An", AssignedHoursAgo: 800),
                    new("Thu thập và làm sạch dữ liệu", "done", "high", -30, "Em", AssignedHoursAgo: 600),
                    new("Huấn luyện mô hình gợi ý", "in_progress", "high", 10, "Em", AssignedHoursAgo: 55),
                    new("Viết chương 3: Phương pháp", "in_progress", "medium", 14, "An", AssignedHoursAgo: 60),
                    new("Chuẩn bị slide bảo vệ", "todo", "low", 40, null, AssignedHoursAgo: 90)
                }),

            // ---------------------------------------------------------------- 10
            new(10, "GYM", "Kế hoạch tập luyện 12 tuần",
                "Giáo án tập luyện, theo dõi cân nặng và thói quen ăn uống theo tuần.",
                "Manager", ProjectStatuses.Running, -20, 64,
                new[] { "An" },
                new TaskSeed[]
                {
                    new("Đo chỉ số cơ thể ban đầu", "done", "medium", -18, "An", AssignedHoursAgo: 480),
                    new("Lên giáo án tuần 1–4", "done", "high", -12, "An", AssignedHoursAgo: 330),
                    new("Tập sức mạnh 3 buổi/tuần", "in_progress", "medium", 7, "An", AssignedHoursAgo: 80),
                    new("Đo lại chỉ số sau 4 tuần", "todo", "low", 9, "An", AssignedHoursAgo: 100)
                }),

            // ---------------------------------------------------------------- 11
            new(11, "TRIP", "Chuyến đi Đà Lạt cuối năm",
                "Lên lịch trình, đặt phòng và phân chia chi phí cho cả nhóm.",
                "Manager", ProjectStatuses.Planning, -5, 40,
                new[] { "An", "Chi", "Dũng", "Giang" },
                new TaskSeed[]
                {
                    new("Chốt ngày đi và ngân sách", "in_progress", "high", 3, "Chi", AssignedHoursAgo: 110),
                    new("Đặt homestay", "todo", "high", 10, "Dũng", AssignedHoursAgo: 108),
                    new("Lên lịch trình 3 ngày 2 đêm", "todo", "medium", 12, "Giang", AssignedHoursAgo: 108),
                    new("Thuê xe và chia chi phí", "todo", "low", 20, null, AssignedHoursAgo: 105)
                }),

            // ---------------------------------------------------------------- 12
            new(12, "BOOK", "Website Bán sách cũ",
                "Đồ án môn Lập trình Web của học kỳ trước, đã nộp và bảo vệ.",
                "Member", ProjectStatuses.Completed, -150, -60,
                new[] { "Bình", "An" },
                new TaskSeed[]
                {
                    new("Thiết kế CSDL", "done", "high", -120, "Bình", AssignedHoursAgo: 2800),
                    new("Làm trang danh sách sách", "done", "medium", -100, "An", AssignedHoursAgo: 2500),
                    new("Làm giỏ hàng", "done", "medium", -85, "Bình", AssignedHoursAgo: 2200),
                    new("Viết báo cáo và slide", "done", "low", -65, "An", AssignedHoursAgo: 1700)
                })
        };

        private static readonly MilestoneSeed[] MilestoneSeeds =
        {
            new(1, 1, "Hoàn thành thiết kế ERD & Database", -2, true),
            new(2, 1, "Triển khai toàn bộ Front-End Views", 3, false),
            new(3, 2, "Báo cáo tiến độ giai đoạn 1", 7, false),
            new(4, 4, "Chốt lộ trình ôn thi 12 tuần", 10, false),
            new(5, 5, "Nộp bản thiết kế ERD", -10, true),
            new(6, 3, "Bàn giao portfolio", -5, true),
            new(7, 6, "Hoàn tất bộ nhận diện chiến dịch", 4, false),
            new(8, 7, "Ra mắt bản beta nội bộ", 20, false),
            new(9, 9, "Nộp bản thảo cho giảng viên hướng dẫn", 21, false),
            new(10, 11, "Chốt danh sách người đi", 7, false),
            new(11, 8, "Mở cổng đăng ký sự kiện", 5, false),
            new(12, 10, "Hoàn thành giai đoạn 1 (4 tuần)", 9, false)
        };

        // =====================================================================
        // TRANG TỔNG QUAN (INDEX): 6 dự án gần nhất + nhiệm vụ gần đây + cột mốc sắp tới
        // =====================================================================
        public static ProjectViewModel BuildIndex()
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var projects = BuildCards(now, today);

            // Cột mốc: chưa xong và gần hạn nhất xếp trước
            var milestones = MilestoneSeeds
                .Select(m => ToMilestone(m, projects.First(p => p.Id == m.ProjectId), today))
                .OrderBy(m => m.IsCompleted).ThenBy(m => m.DueDate)
                .Take(6)
                .ToList();

            // Gộp nhiệm vụ của mọi dự án để lấy danh sách "gần đây"
            var allTasks = Projects
                .SelectMany(p => BuildTasks(p, now).Select(t => (Project: p, Task: t)))
                .ToList();

            var assignedToMe = allTasks
                .Where(x => x.Task.AssigneeName == CurrentUser && x.Task.StatusKey != "done")
                .OrderByDescending(x => x.Task.AssignedAt)
                .Take(6)
                .Select(x => ToRecent(x.Project, x.Task, x.Task.ReporterName,
                    x.Task.ReporterName == CurrentUser ? "Tự giao" : $"Giao bởi {x.Task.ReporterName}"))
                .ToList();

            var assignedByMe = allTasks
                .Where(x => x.Task.ReporterName == CurrentUser
                            && !string.IsNullOrEmpty(x.Task.AssigneeName)
                            && x.Task.AssigneeName != CurrentUser
                            && x.Task.StatusKey != "done")
                .OrderByDescending(x => x.Task.AssignedAt)
                .Take(6)
                .Select(x => ToRecent(x.Project, x.Task, x.Task.AssigneeName, $"Giao cho {x.Task.AssigneeName}"))
                .ToList();

            var model = NewModel(projects);
            model.RecentProjects = projects.Take(RecentProjectCount).ToList();
            model.RecentMilestones = milestones;
            model.AssignedToMe = assignedToMe;
            model.AssignedByMe = assignedByMe;
            return model;
        }

        // =====================================================================
        // TRANG TẤT CẢ DỰ ÁN (tìm kiếm, lọc, sắp xếp, phân trang ở trình duyệt)
        // =====================================================================
        public static ProjectViewModel BuildAll()
        {
            return NewModel(BuildCards(DateTime.Now, DateTime.Today));
        }

        // =====================================================================
        // TRANG CHI TIẾT (KANBAN). Trả về null nếu không có dự án này.
        // =====================================================================
        public static ProjectDetailViewModel? BuildDetail(int id)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var seed = Projects.FirstOrDefault(p => p.Id == id);
            if (seed == null) return null;

            var card = ToCard(seed, today);
            var canManage = seed.Role == "Manager";
            var canEdit = seed.Role is "Manager" or "Member";
            var tasks = BuildTasks(seed, now);

            // Thành viên: bạn giữ vai trò của dự án; nếu bạn không phải Manager thì người đầu tiên còn lại là Manager
            var members = seed.Members.Select(n => new ProjectMemberItem
            {
                UserId = Users[n].Id,
                Name = n,
                Email = Users[n].Email,
                Role = n == CurrentUser ? seed.Role : (n == ManagerName(seed) ? "Manager" : "Member"),
                OpenTaskCount = seed.Tasks.Count(t => t.Assignee == n && t.Status != "done"),
                IsCurrentUser = n == CurrentUser
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
                    CreatedAt = now.AddHours(-3)
                });
            }

            var milestones = MilestoneSeeds
                .Where(m => m.ProjectId == seed.Id)
                .Select(m => ToMilestone(m, card, today))
                .OrderBy(m => m.DueDate)
                .ToList();

            // Hoạt động dự án: gộp lịch sử của các nhiệm vụ
            var activities = tasks
                .SelectMany(t => t.History.Select(h => new ProjectActivityItem
                {
                    ActorName = h.ActorName,
                    Text = $"{h.Text} ({t.Key})",
                    CreatedAt = h.CreatedAt
                }))
                .OrderByDescending(a => a.CreatedAt)
                .Take(8)
                .ToList();

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
                CurrentUserId = Users[CurrentUser].Id,
                CurrentUserName = CurrentUser,
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
                Activities = activities
            };
        }

        // =====================================================================
        // HÀM DÙNG CHUNG
        // =====================================================================

        /// <summary>Thẻ dự án, sắp theo hoạt động gần nhất (mới nhất trước).</summary>
        private static List<ProjectCardItem> BuildCards(DateTime now, DateTime today)
        {
            var cards = Projects.Select(p =>
                {
                    var card = ToCard(p, today);
                    var tasks = BuildTasks(p, now);
                    card.LastActivityAt = tasks.Count == 0 ? today.AddDays(p.StartDays) : tasks.Max(t => t.UpdatedAt);
                    return card;
                })
                .OrderByDescending(c => c.LastActivityAt)
                .ToList();

            for (var i = 0; i < cards.Count; i++) cards[i].SortIndex = i;
            return cards;
        }

        private static ProjectViewModel NewModel(List<ProjectCardItem> projects)
        {
            return new ProjectViewModel
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.StatusKey == "running"),
                CompletedProjects = projects.Count(p => p.StatusKey == "completed"),
                TotalTeamMembers = projects.SelectMany(p => p.MemberNames).Distinct().Count(),
                Projects = projects,
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

        /// <summary>Trưởng nhóm của dự án: chính bạn nếu bạn là Manager, ngược lại là người đầu tiên khác bạn.</summary>
        private static string ManagerName(ProjectSeed s) =>
            s.Role == "Manager" ? CurrentUser : s.Members.First(n => n != CurrentUser);

        private static DateTime Clamp(DateTime value, DateTime now) => value > now ? now : value;

        private static List<TaskCardItem> BuildTasks(ProjectSeed seed, DateTime now)
        {
            var manager = ManagerName(seed);

            return seed.Tasks.Select((t, i) =>
            {
                var taskId = seed.Id * 100 + i + 1;
                var due = t.DueDays.HasValue ? now.Date.AddDays(t.DueDays.Value) : (DateTime?)null;
                var hasAssignee = !string.IsNullOrEmpty(t.Assignee) && Users.ContainsKey(t.Assignee);
                var reporter = t.Reporter ?? manager;
                var assignedAt = now.AddHours(-t.AssignedHoursAgo);
                var comments = t.Comments ?? Array.Empty<CommentSeed>();
                var history = BuildHistory(t, reporter, manager, assignedAt, now, comments);

                return new TaskCardItem
                {
                    Id = taskId,
                    ProjectId = seed.Id,
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
                    ReporterName = reporter,
                    DueDate = due,
                    IsOverdue = due.HasValue && due.Value.Date < now.Date && t.Status != "done",
                    AssignedAt = assignedAt,
                    CreatedAt = assignedAt.AddMinutes(-5),
                    UpdatedAt = history.First().CreatedAt,
                    CompletedAt = t.Status == "done" ? history.First().CreatedAt : (DateTime?)null,
                    SubTasks = (t.Subs ?? Array.Empty<SubSeed>())
                        .Select((s, j) => new SubTaskItem { Id = taskId * 10 + j + 1, Title = s.Title, IsDone = s.Done })
                        .ToList(),
                    Comments = comments
                        .OrderByDescending(c => c.HoursAgo)
                        .Select((c, k) => new TaskCommentItem
                        {
                            Id = taskId * 100 + k + 1,
                            AuthorName = c.Author,
                            Text = c.Text,
                            CreatedAt = now.AddHours(-c.HoursAgo),
                            IsRework = c.IsRework
                        })
                        .ToList(),
                    History = history
                };
            }).ToList();
        }

        /// <summary>Dựng lịch sử nhiệm vụ từ trạng thái hiện tại (mới nhất trước).</summary>
        private static List<TaskHistoryItem> BuildHistory(TaskSeed t, string reporter, string manager,
            DateTime assignedAt, DateTime now, CommentSeed[] comments)
        {
            var hasAssignee = !string.IsNullOrEmpty(t.Assignee);
            var worker = hasAssignee ? t.Assignee! : reporter;
            var rework = comments.FirstOrDefault(c => c.IsRework);

            var list = new List<TaskHistoryItem>
            {
                new() { ActorName = reporter, Text = "đã tạo nhiệm vụ", CreatedAt = assignedAt.AddMinutes(-5) }
            };

            if (hasAssignee)
                list.Add(new() { ActorName = reporter, Text = $"đã giao cho {t.Assignee}", CreatedAt = assignedAt });

            if (t.Status != "todo" || rework != null)
                list.Add(Moved(worker, "Đang làm", Clamp(assignedAt.AddHours(2), now)));

            if (t.Status is "review" or "done" || rework != null)
            {
                var reviewAt = rework != null ? now.AddHours(-rework.HoursAgo - 6) : assignedAt.AddHours(6);
                list.Add(Moved(worker, "Chờ duyệt", Clamp(reviewAt, now)));
            }

            if (rework != null)
                list.Add(new()
                {
                    ActorName = rework.Author,
                    Text = "đã trả nhiệm vụ về \"Đang làm\" để làm lại",
                    CreatedAt = now.AddHours(-rework.HoursAgo)
                });

            if (t.Status == "done")
                list.Add(new()
                {
                    ActorName = manager,
                    Text = "đã duyệt và chuyển sang \"Hoàn thành\"",
                    CreatedAt = Clamp(assignedAt.AddHours(10), now)
                });

            return list.OrderByDescending(h => h.CreatedAt).ToList();
        }

        private static TaskHistoryItem Moved(string actor, string column, DateTime at) => new()
        {
            ActorName = actor,
            Text = $"đã chuyển sang \"{column}\"",
            CreatedAt = at
        };

        private static RecentTaskItem ToRecent(ProjectSeed p, TaskCardItem t, string person, string label) => new()
        {
            TaskId = t.Id,
            TaskKey = t.Key,
            Title = t.Title,
            ProjectId = p.Id,
            ProjectName = p.Name,
            StatusKey = t.StatusKey,
            StatusLabel = t.StatusLabel,
            PriorityKey = t.PriorityKey,
            PriorityLabel = t.PriorityLabel,
            DueDate = t.DueDate,
            IsOverdue = t.IsOverdue,
            PersonName = person,
            PersonLabel = label,
            AssignedAt = t.AssignedAt
        };

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