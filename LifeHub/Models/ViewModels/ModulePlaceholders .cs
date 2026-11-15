using LifeHub.Models.ViewModels;

namespace LifeHub.Data.Mock
{
    /// <summary>Mock data cho các trang giữ chỗ. Nội dung lấy từ brief và sơ đồ bảng LifeHub.</summary>
    public static class ModulePlaceholders
    {
        private static ComingSoonViewModel Create(string key, string title, string summary, string[] features, string[] tables) => new()
        {
            ActivePage = key,
            Title = title,
            Summary = summary,
            Features = features.ToList(),
            Tables = tables.ToList()
        };

        public static ComingSoonViewModel Study() => Create("Study", "Học tập",
            "Quản lý môn học, thời khóa biểu, bài tập, lịch thi và điểm số.",
            new[] { "Môn học và thời khóa biểu theo tuần", "Bài tập, kiểm tra, lịch thi", "Điểm số và GPA theo học kỳ", "Tiến độ hoàn thành từng môn" },
            new[] { "semesters", "courses", "course_schedules", "assignments" });

        public static ComingSoonViewModel Task() => Create("Task", "Công việc",
            "Quản lý việc cá nhân và việc được giao, có bảng Kanban.",
            new[] { "Danh sách việc theo trạng thái và mức ưu tiên", "Kanban 4 cột: cần làm, đang làm, chờ duyệt, hoàn thành", "Deadline, việc con, việc lặp lại", "Bình luận và nhãn" },
            new[] { "tasks", "task_comments", "tags", "task_tags" });

        public static ComingSoonViewModel Project() => Create("Project", "Dự án",
            "Làm việc nhóm: thành viên, phân công, tiến độ và cột mốc.",
            new[] { "Tạo dự án, chia nhiệm vụ nhỏ", "Thành viên Manager, Member, Viewer và lời mời qua email", "Cột mốc và Timeline", "Nhật ký hoạt động dự án" },
            new[] { "projects", "project_members", "project_invitations", "project_milestones", "project_activities" });

        public static ComingSoonViewModel Time() => Create("Time", "Lịch và thời gian",
            "Một lịch chung cho lịch học, deadline, dự án và sự kiện cá nhân.",
            new[] { "Xem lịch theo ngày, tuần, tháng", "Tạo sự kiện và lịch hẹn nhắc", "Tự gộp lịch học, lịch thi, deadline", "Pomodoro" },
            new[] { "events", "reminders", "pomodoro_sessions" });

        public static ComingSoonViewModel Finance() => Create("Finance", "Tài chính",
            "Theo dõi thu chi, ví, ngân sách và mục tiêu tiết kiệm.",
            new[] { "Thu, chi và chuyển giữa các ví", "Ngân sách theo danh mục, cảnh báo khi gần hết hạn mức", "Mục tiêu tiết kiệm", "Biểu đồ thu chi theo thời gian" },
            new[] { "wallets", "categories", "transactions", "budgets", "savings_goals" });

        public static ComingSoonViewModel Goal() => Create("Goal", "Mục tiêu",
            "Đặt mục tiêu ngắn hạn và dài hạn, đo bằng các chỉ số cụ thể.",
            new[] { "Mục tiêu theo nhóm: học tập, sự nghiệp, sức khỏe, tài chính", "Kết quả then chốt đo được", "Tiến độ tính từ chỉ số và việc gắn kèm", "Tự đánh giá khi kết thúc" },
            new[] { "goals", "key_results" });

        public static ComingSoonViewModel Habit() => Create("Habit", "Thói quen",
            "Điểm danh mỗi ngày và giữ chuỗi ngày liên tiếp.",
            new[] { "Tạo thói quen theo ngày hoặc tuần", "Điểm danh, hỗ trợ thói quen định lượng", "Chuỗi hiện tại và kỷ lục", "Tỷ lệ duy trì" },
            new[] { "habits", "habit_logs" });

        public static ComingSoonViewModel Knowledge() => Create("Knowledge", "Ghi chú và tài liệu",
            "Ghi chú, thư viện tài liệu và chia sẻ cho người khác.",
            new[] { "Ghi chú theo thư mục, ghim lên đầu", "Tải lên và quản lý tài liệu", "Gắn với môn học hoặc dự án", "Chia sẻ quyền xem hoặc sửa" },
            new[] { "note_categories", "notes", "note_tags", "documents", "shared_items" });

        public static ComingSoonViewModel Lifestyle() => Create("Lifestyle", "Sinh hoạt",
            "Nhật ký mỗi ngày về giấc ngủ, vận động, tâm trạng và năng suất.",
            new[] { "Giờ ngủ, nước uống, vận động, cân nặng, tâm trạng", "Biểu đồ sức khỏe theo tuần và tháng", "Điểm năng suất mỗi ngày", "Huy hiệu thành tích" },
            new[] { "lifestyle_logs", "productivity_scores", "achievements", "user_achievements" });

        public static ComingSoonViewModel Notification() => Create("Notification", "Thông báo",
            "Tất cả thông báo của bạn, bấm vào để mở đúng trang liên quan.",
            new[] { "Phân biệt đã đọc và chưa đọc", "Mở đúng đối tượng liên quan (ví dụ task số 15)", "Nhắc theo lịch hẹn" },
            new[] { "notifications", "reminders" });

        public static ComingSoonViewModel Settings() => Create("Settings", "Cài đặt",
            "Tùy chỉnh giao diện, ngôn ngữ, thông báo và Pomodoro.",
            new[] { "Chế độ sáng hoặc tối, ngôn ngữ, múi giờ", "Bật hoặc tắt thông báo", "Thời gian học và nghỉ của Pomodoro", "Bố cục Dashboard và phân hệ hiện ở sidebar" },
            new[] { "user_settings" });

        public static ComingSoonViewModel AI() => Create("AI", "Trợ lý AI",
            "Hỏi đáp và gợi ý kế hoạch dựa trên dữ liệu của bạn.",
            new[] { "Danh sách cuộc trò chuyện", "Hỏi đáp trong từng cuộc trò chuyện" },
            new[] { "ai_conversations", "ai_messages" });
    }
}