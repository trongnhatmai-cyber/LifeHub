using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.TaskManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho phân hệ Công việc. Một số việc cố ý khớp với StudyMockData
    /// (đồ án IT301, bài SQL quá hạn IT302, thi giữa kỳ MA201...) để hai phân hệ nhất quán.
    /// Ngày tháng tính theo hôm nay nên trạng thái "quá hạn" luôn đúng.
    /// </summary>
    public static class TaskMockData
    {
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

            TaskItemViewModel T(int id, string title, string description, string status, string priority,
                int dayOffset, string project, string tags = "", int? estimate = null,
                int subDone = 0, int subTotal = 0, int comments = 0) => new()
            {
                Id = id,
                Title = title,
                Description = description,
                Status = status,
                Priority = priority,
                Deadline = today.AddDays(dayOffset),
                ProjectName = project,
                EstimatedMinutes = estimate,
                SubtaskDone = subDone,
                SubtaskTotal = subTotal,
                CommentCount = comments,
                Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(n => new TaskTagItem { Name = n, Color = TagColors.GetValueOrDefault(n, "#ea580c") })
                    .ToList()
            };

            return new TaskViewModel
            {
                Tasks = new List<TaskItemViewModel>
                {
                    // CẦN LÀM
                    T(5, "Ôn thi giữa kỳ Xác suất thống kê", "Làm lại bài tập chương 3, 4 và tổng hợp công thức.", "todo", "high", 4, "MA201", "Ôn thi,Học tập", 180, 0, 3),
                    T(6, "Đọc tài liệu Entity Framework Core", "Tìm hiểu DbContext, Migrations và Fluent API trong C#.", "todo", "medium", 3, "Cá nhân", "Tự học,Back-end", 90),
                    T(7, "Làm quiz từ vựng chuyên ngành", "Ôn 50 từ vựng trước giờ kiểm tra nhanh.", "todo", "medium", 1, "EN202", "Học tập", 30),

                    // ĐANG LÀM
                    T(1, "Hoàn thiện Razor View Dashboard", "Đồng bộ giao diện Dashboard và Study theo chuẩn tone màu pastel.", "in_progress", "urgent", 1, "LIFEHUB", "Giao diện", 120, 2, 4, 3),
                    T(3, "Làm bài tập truy vấn SQL nâng cao", "Hoàn thành các câu JOIN, GROUP BY và truy vấn lồng.", "in_progress", "urgent", -2, "IT302", "Học tập,Cơ sở dữ liệu", 90, 1, 3, 1),
                    T(4, "Đồ án giữa kỳ: Web quản lý", "Hoàn thành CRUD và phân quyền cho đồ án môn Lập trình Web.", "in_progress", "high", 6, "IT301", "Đồ án,Học tập", 600, 2, 5, 2),
                    T(8, "Thiết kế bảng dữ liệu Kanban Task", "Tối ưu bảng tasks, tags, task_tags và thứ tự sắp xếp.", "in_progress", "medium", 4, "LIFEHUB", "Cơ sở dữ liệu,Back-end", 120),

                    // CHỜ DUYỆT
                    T(9, "Kiểm tra responsive trên Mobile", "Đảm bảo sidebar chuyển thành thanh ngang trên điện thoại.", "review", "low", 5, "LIFEHUB", "Giao diện", 45, 3, 3),
                    T(10, "Viết báo cáo phân tích yêu cầu", "Báo cáo use case và đặc tả yêu cầu cho môn Công nghệ phần mềm.", "review", "medium", 8, "SE303", "Học tập,Đồ án", 240, 4, 4, 5),

                    // ĐÃ XONG
                    T(2, "Tạo các ViewModel strongly-typed", "Tạo lớp ViewModel trong Models/ViewModels cho từng phân hệ.", "done", "high", 0, "LIFEHUB", "Back-end", 60, 5, 5),
                    T(11, "Nộp bài tập UML", "Vẽ sơ đồ lớp và sơ đồ tuần tự cho hệ thống thư viện.", "done", "medium", -8, "SE303", "Học tập"),
                    T(12, "Cài đặt môi trường SQL Server", "Cài SQL Server và SSMS, kiểm tra kết nối từ project.", "done", "low", -15, "Cá nhân", "Tự học,Cơ sở dữ liệu", 40)
                }
            };
        }
    }
}