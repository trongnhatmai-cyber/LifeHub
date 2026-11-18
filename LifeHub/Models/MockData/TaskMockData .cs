using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.TaskManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho phân hệ Công việc. Một số việc cố ý khớp với StudyMockData
    /// (đồ án IT301, bài SQL quá hạn IT302, thi giữa kỳ MA201...) để các phân hệ nhất quán.
    /// Việc có ProjectId là việc của dự án; ProjectId trống là việc cá nhân.
    /// </summary>
    public static class TaskMockData
    {
        private const string Me = "Bạn";
        private const string An = "Nguyễn Văn An";
        private const string Binh = "Trần Thị Bình";

        private static readonly Dictionary<string, string> TagColors = new()
        {
            ["Giao diện"] = "#ea580c",
            ["Back-end"] = "#2563eb",
            ["Cơ sở dữ liệu"] = "#0891b2",
            ["Học tập"] = "#7c3aed",
            ["Đồ án"] = "#db2777",
            ["Ôn thi"] = "#d97706",
            ["Tự học"] = "#16a34a"
        };

        public static TaskViewModel GetTaskViewModel()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var projects = new List<TaskProjectOption>
            {
                new() { Id = 1, Name = "LIFEHUB", Members = new() {
                    new() { UserId = 1, Name = Me, Role = "manager" },
                    new() { UserId = 2, Name = An, Role = "member" },
                    new() { UserId = 3, Name = Binh, Role = "member" } } },
                new() { Id = 2, Name = "Đồ án Lập trình Web", Members = new() {
                    new() { UserId = 2, Name = An, Role = "manager" },
                    new() { UserId = 1, Name = Me, Role = "member" },
                    new() { UserId = 3, Name = Binh, Role = "member" } } },
                new() { Id = 3, Name = "Báo cáo CNPM nhóm 5", Members = new() {
                    new() { UserId = 3, Name = Binh, Role = "manager" },
                    new() { UserId = 1, Name = Me, Role = "member" } } }
            };

            TaskItemViewModel T(int id, string title, string description, string status, string priority, int dayOffset,
                string tags = "", int? estimate = null, int subDone = 0, int subTotal = 0,
                int? projectId = null, string course = "", string assignedBy = "", string assignee = Me) => new()
            {
                Id = id, Title = title, Description = description, Status = status, Priority = priority,
                Deadline = today.AddDays(dayOffset),
                ProjectId = projectId,
                ProjectName = projectId.HasValue ? projects.First(p => p.Id == projectId.Value).Name : string.Empty,
                CourseCode = course, AssignedBy = assignedBy, AssignedTo = assignee,
                EstimatedMinutes = estimate, SubtaskDone = subDone, SubtaskTotal = subTotal,
                Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(n => new TaskTagItem { Name = n, Color = TagColors.GetValueOrDefault(n, "#ea580c") })
                    .ToList()
            };

            var tasks = new List<TaskItemViewModel>
            {
                // CẦN LÀM
                T(5, "Ôn thi giữa kỳ Xác suất thống kê", "Làm lại bài tập chương 3, 4 và tổng hợp công thức.", "todo", "high", 4, "Ôn thi,Học tập", 180, 0, 3, course: "MA201"),
                T(6, "Đọc tài liệu Entity Framework Core", "Tìm hiểu DbContext, Migrations và Fluent API trong C#.", "todo", "medium", 3, "Tự học,Back-end", 90),
                T(7, "Làm quiz từ vựng chuyên ngành", "Ôn 50 từ vựng trước giờ kiểm tra nhanh.", "todo", "medium", 1, "Học tập", 30, course: "EN202"),
                T(13, "Viết module đăng nhập", "Đăng ký, đăng nhập và phân quyền cho đồ án Lập trình Web.", "todo", "high", 7, "Back-end,Đồ án", 240, projectId: 2, course: "IT301", assignedBy: An),

                // ĐANG LÀM
                T(1, "Hoàn thiện Razor View Dashboard", "Đồng bộ giao diện Dashboard và Study theo chuẩn tone màu pastel.", "in_progress", "urgent", 1, "Giao diện", 120, 2, 4, projectId: 1),
                T(3, "Làm bài tập truy vấn SQL nâng cao", "Hoàn thành các câu JOIN, GROUP BY và truy vấn lồng.", "in_progress", "urgent", -2, "Học tập,Cơ sở dữ liệu", 90, 1, 3, course: "IT302"),
                T(4, "Đồ án giữa kỳ: Web quản lý", "Hoàn thành CRUD và phân quyền cho đồ án môn Lập trình Web.", "in_progress", "high", 6, "Đồ án,Học tập", 600, 2, 5, projectId: 2, course: "IT301", assignedBy: An),
                T(8, "Thiết kế bảng dữ liệu Kanban Task", "Tối ưu bảng tasks, tags, task_tags và thứ tự sắp xếp.", "in_progress", "medium", 4, "Cơ sở dữ liệu,Back-end", 120, projectId: 1),
                T(14, "Thiết kế bảng project_members", "Bảng thành viên dự án với vai trò manager, member, viewer.", "in_progress", "high", 3, "Cơ sở dữ liệu", 90, projectId: 1, assignee: Binh),

                // CHỜ DUYỆT
                T(9, "Kiểm tra responsive trên Mobile", "Đảm bảo sidebar chuyển thành thanh ngang trên điện thoại.", "review", "low", 5, "Giao diện", 45, 3, 3, projectId: 1),
                T(10, "Viết báo cáo phân tích yêu cầu", "Báo cáo use case và đặc tả yêu cầu cho môn Công nghệ phần mềm.", "review", "medium", 8, "Học tập,Đồ án", 240, 4, 4, projectId: 3, course: "SE303", assignedBy: Binh),

                // ĐÃ XONG
                T(2, "Tạo các ViewModel strongly-typed", "Tạo lớp ViewModel trong Models/ViewModels cho từng phân hệ.", "done", "high", 0, "Back-end", 60, 5, 5, projectId: 1),
                T(11, "Nộp bài tập UML", "Vẽ sơ đồ lớp và sơ đồ tuần tự cho hệ thống thư viện.", "done", "medium", -8, "Học tập", course: "SE303"),
                T(12, "Cài đặt môi trường SQL Server", "Cài SQL Server và SSMS, kiểm tra kết nối từ project.", "done", "low", -15, "Tự học,Cơ sở dữ liệu", 40)
            };

            // Bình luận mẫu (bảng task_comments)
            TaskCommentItem C(int id, string author, string content, int hoursAgo) =>
                new() { Id = id, AuthorName = author, Content = content, CreatedAt = now.AddHours(-hoursAgo) };

            var comments = new Dictionary<int, List<TaskCommentItem>>
            {
                [1] = new() { C(1, An, "Phần sidebar nhớ giữ đúng màu theo từng phân hệ nhé.", 30), C(2, Me, "Ok, mình đang chỉnh lại phần dark mode.", 26), C(3, Binh, "Xong thì gửi mình review phần responsive.", 5) },
                [3] = new() { C(4, Me, "Câu 5 phải dùng truy vấn lồng, mình đang làm dở.", 20) },
                [4] = new() { C(5, An, "Deadline nộp đồ án là thứ Sáu tuần sau, cố gắng xong CRUD trước.", 48), C(6, Me, "Mình xong phần sản phẩm và danh mục rồi.", 10) },
                [10] = new() { C(7, Binh, "Em xem lại sơ đồ use case giúp chị.", 72), C(8, Me, "Em đã bổ sung use case đặt phòng.", 36), C(9, Binh, "Ổn rồi, chị chuyển sang chờ duyệt.", 8) }
            };
            foreach (var t in tasks)
            {
                if (comments.TryGetValue(t.Id, out var list)) t.Comments = list;
            }

            return new TaskViewModel { Tasks = tasks, Projects = projects };
        }
    }
}