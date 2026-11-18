using LifeHub.Models.ViewModels.GoalManagement;

namespace LifeHub.Models.MockData
{
    // Nguồn dữ liệu trình diễn duy nhất cho phân hệ Goal & Habit.
    // Khi kết nối dữ liệu thật, thay phần cung cấp dữ liệu tại Controller/Service,
    // giữ nguyên ViewModel và Razor View.
    public static class GoalHabitMockData
    {
        private static List<PageMetricViewModel> GoalMetrics() => new()
        {
            new() { Label = "Mục tiêu đang thực hiện", Value = "04", Hint = "+1 so với tháng trước", Icon = "◎" },
            new() { Label = "Tiến độ trung bình", Value = "68%", Hint = "Tăng 8% trong tháng", Icon = "↗" },
            new() { Label = "Kết quả then chốt", Value = "08", Hint = "5 KR đang đúng tiến độ", Icon = "◇" },
            new() { Label = "Sắp đến hạn", Value = "02", Hint = "Trong 7 ngày tới", Icon = "◷" }
        };

        private static List<PageMetricViewModel> HabitMetrics() => new()
        {
            new() { Label = "Thói quen đang theo dõi", Value = "06", Hint = "5 thói quen hằng ngày", Icon = "↻" },
            new() { Label = "Hoàn thành hôm nay", Value = "04/06", Hint = "Đạt 67% kế hoạch", Icon = "✓" },
            new() { Label = "Streak hiện tại tốt nhất", Value = "12 ngày", Hint = "Học từ vựng tiếng Anh", Icon = "🔥" },
            new() { Label = "Tỷ lệ duy trì", Value = "82%", Hint = "Trong 30 ngày gần nhất", Icon = "◌" }
        };

        private static List<PageItemViewModel> Goals() => new()
        {
            new() { Title = "Đạt GPA giỏi kỳ này (≥ 3.6)", Subtitle = "Duy trì kết quả học tập ổn định", Category = "Học tập", Status = "Đang thực hiện", Progress = 85, DueDate = "15/12/2026", Value = "85%" },
            new() { Title = "Xây dựng nền tảng LIFEHUB hoàn chỉnh", Subtitle = "Hoàn thiện giao diện và tài liệu đồ án", Category = "Sự nghiệp", Status = "Đang thực hiện", Progress = 75, DueDate = "25/11/2026", Value = "75%" },
            new() { Title = "Tiết kiệm 20 triệu mua Laptop", Subtitle = "Tích lũy đều đặn mỗi tháng", Category = "Tài chính", Status = "Đúng tiến độ", Progress = 25, DueDate = "18/05/2027", Value = "5/20 triệu" },
            new() { Title = "Đọc 12 cuốn sách trong năm", Subtitle = "Phát triển kiến thức cá nhân", Category = "Cá nhân", Status = "Cần chú ý", Progress = 42, DueDate = "31/12/2026", Value = "5/12 cuốn" }
        };

        private static List<PageItemViewModel> Habits() => new()
        {
            new() { Title = "Đọc sách 20 phút mỗi sáng", Subtitle = "07:00 · Mỗi ngày", Category = "Phát triển bản thân", Status = "Đã hoàn thành", Progress = 100, Value = "7 ngày streak" },
            new() { Title = "Uống đủ 2 lít nước", Subtitle = "Chia đều trong ngày", Category = "Sức khỏe", Status = "Chưa hoàn thành", Progress = 50, Value = "4 ngày streak" },
            new() { Title = "Tập thể dục 30 phút", Subtitle = "17:30 · Mỗi ngày", Category = "Sức khỏe", Status = "Chưa hoàn thành", Progress = 0, Value = "3 ngày streak" },
            new() { Title = "Học 20 từ vựng tiếng Anh", Subtitle = "20:00 · Mỗi ngày", Category = "Học tập", Status = "Đã hoàn thành", Progress = 100, Value = "12 ngày streak" },
            new() { Title = "Đi ngủ trước 23:30", Subtitle = "23:00 · Mỗi ngày", Category = "Sức khỏe", Status = "Chưa hoàn thành", Progress = 0, Value = "5 ngày streak" },
            new() { Title = "Viết nhật ký cuối ngày", Subtitle = "21:30 · Thứ 2 đến Thứ 6", Category = "Cá nhân", Status = "Đã hoàn thành", Progress = 100, Value = "6 ngày streak" }
        };

        private static List<PageItemViewModel> KeyResults() => new()
        {
            new() { Title = "Hoàn thành 100% bài tập các môn", Subtitle = "Mục tiêu: Đạt GPA giỏi kỳ này", Category = "Học tập", Status = "Đúng tiến độ", Progress = 86, Value = "12 / 14 bài" },
            new() { Title = "Điểm thi giữa kỳ đạt ≥ 8.5", Subtitle = "Mục tiêu: Đạt GPA giỏi kỳ này", Category = "Học tập", Status = "Hoàn thành", Progress = 90, Value = "9 / 10 điểm" },
            new() { Title = "Xây dựng 10 phân hệ giao diện", Subtitle = "Mục tiêu: Xây dựng LIFEHUB", Category = "Sự nghiệp", Status = "Đang thực hiện", Progress = 60, Value = "6 / 10 module" },
            new() { Title = "Hoàn thành tài liệu báo cáo đồ án", Subtitle = "Mục tiêu: Xây dựng LIFEHUB", Category = "Sự nghiệp", Status = "Hoàn thành", Progress = 100, Value = "1 / 1 báo cáo" },
            new() { Title = "Quỹ tiết kiệm hiện tại", Subtitle = "Mục tiêu: Mua Laptop", Category = "Tài chính", Status = "Đang thực hiện", Progress = 25, Value = "5 / 20 triệu" }
        };

        public static GoalHabitPageViewModel Get(string page)
        {
            var vm = new GoalHabitPageViewModel
            {
                Eyebrow = "LIFEHUB · GOAL MANAGEMENT",
                Accent = page.StartsWith("Habit") ? "habit" : "goal",
                Metrics = page.StartsWith("Habit") ? HabitMetrics() : GoalMetrics(),
                Chart = new() {
                    new() { Label = "T7", Value = 42 }, new() { Label = "CN", Value = 55 },
                    new() { Label = "T2", Value = 48 }, new() { Label = "T3", Value = 64 },
                    new() { Label = "T4", Value = 58 }, new() { Label = "T5", Value = 72 },
                    new() { Label = "T6", Value = 82 }
                },
                Filters = new() { "Tất cả", "Đang thực hiện", "Hoàn thành", "Cần chú ý" },
                Items = Goals(),
                SectionTitle = "Danh sách mục tiêu",
                SectionDescription = "Theo dõi các kế hoạch và kết quả cần đạt được.",
                SecondaryTitle = "Mục tiêu nổi bật",
                SecondaryItems = Goals().Take(3).ToList()
            };

            switch (page)
            {
                case "GoalList":
                    vm.Title = "Mục tiêu cá nhân";
                    vm.Description = "Tổ chức các mục tiêu ngắn hạn và dài hạn theo từng lĩnh vực.";
                    vm.PrimaryActionLabel = "＋ Tạo mục tiêu";
                    vm.SectionTitle = "Tất cả mục tiêu";
                    break;
                case "KeyResult":
                    vm.Title = "Kết quả then chốt (KR)";
                    vm.Description = "Đo lường mức độ thành công của từng mục tiêu bằng kết quả cụ thể.";
                    vm.PrimaryActionLabel = "＋ Thêm kết quả KR";
                    vm.Items = KeyResults();
                    vm.SectionTitle = "Các kết quả then chốt";
                    vm.SectionDescription = "Giá trị hiện tại so với chỉ tiêu đã thiết lập.";
                    vm.SecondaryTitle = "Phân bổ KR theo mục tiêu";
                    vm.SecondaryItems = new() {
                        new() { Title = "Đạt GPA giỏi kỳ này", Subtitle = "2 kết quả then chốt", Value = "2 KR", Progress = 88 },
                        new() { Title = "Xây dựng LIFEHUB", Subtitle = "2 kết quả then chốt", Value = "2 KR", Progress = 80 },
                        new() { Title = "Tiết kiệm mua Laptop", Subtitle = "1 kết quả then chốt", Value = "1 KR", Progress = 25 }
                    };
                    break;
                case "GoalProgress":
                    vm.Title = "Tiến độ mục tiêu";
                    vm.Description = "Quan sát xu hướng hoàn thành, cột mốc và những mục tiêu cần ưu tiên.";
                    vm.PrimaryActionLabel = "Xem mục tiêu";
                    vm.SectionTitle = "Tiến độ từng mục tiêu";
                    vm.SectionDescription = "Phần trăm hoàn thành được tổng hợp từ các kết quả then chốt.";
                    break;
                case "GoalReview":
                    vm.Title = "Đánh giá kết quả";
                    vm.Description = "Tổng kết kết quả đạt được và rút ra bài học cho chu kỳ tiếp theo.";
                    vm.PrimaryActionLabel = "＋ Tạo bản đánh giá";
                    vm.SectionTitle = "Tổng kết mục tiêu";
                    vm.SectionDescription = "Bản tổng kết minh họa cho chu kỳ hiện tại.";
                    vm.Items = new() {
                        new() { Title = "Đạt GPA giỏi kỳ này", Subtitle = "Tiến độ tốt, còn 2 đầu việc cần hoàn tất", Category = "Học tập", Status = "Gần đạt", Progress = 85, Value = "85%" },
                        new() { Title = "Xây dựng nền tảng LIFEHUB", Subtitle = "Đúng kế hoạch phát triển", Category = "Sự nghiệp", Status = "Tích cực", Progress = 75, Value = "75%" },
                        new() { Title = "Tiết kiệm 20 triệu mua Laptop", Subtitle = "Cần duy trì khoản tiết kiệm định kỳ", Category = "Tài chính", Status = "Cần cải thiện", Progress = 25, Value = "25%" }
                    };
                    break;
                case "HabitOverview":
                    vm.Title = "Tổng quan thói quen";
                    vm.Eyebrow = "LIFEHUB · HABIT MANAGEMENT";
                    vm.Description = "Xây dựng nhịp sinh hoạt tích cực và duy trì những thay đổi nhỏ mỗi ngày.";
                    vm.PrimaryActionLabel = "＋ Tạo thói quen";
                    vm.Items = Habits().Take(4).ToList();
                    vm.SectionTitle = "Thói quen hôm nay";
                    vm.SectionDescription = "Danh sách thói quen cần thực hiện trong ngày.";
                    vm.SecondaryTitle = "Thói quen nổi bật";
                    vm.SecondaryItems = Habits().Where(x => x.Progress == 100).ToList();
                    break;
                case "HabitList":
                    vm.Title = "Danh sách thói quen";
                    vm.Eyebrow = "LIFEHUB · HABIT MANAGEMENT";
                    vm.Description = "Quản lý lịch thực hiện, nhóm thói quen và trạng thái duy trì.";
                    vm.PrimaryActionLabel = "＋ Thêm thói quen";
                    vm.Items = Habits();
                    vm.SectionTitle = "Tất cả thói quen";
                    vm.SectionDescription = "6 thói quen đang được theo dõi.";
                    break;
                case "HabitTracking":
                    vm.Title = "Theo dõi hằng ngày";
                    vm.Eyebrow = "LIFEHUB · DAILY CHECK-IN";
                    vm.Description = "Ghi nhận tiến độ thực hiện thói quen trong ngày.";
                    vm.PrimaryActionLabel = "Xem lịch sử";
                    vm.Items = Habits();
                    vm.SectionTitle = "Checklist hôm nay";
                    vm.SectionDescription = "Trạng thái mẫu được cung cấp từ C# MockData.";
                    break;
                case "HabitStreak":
                    vm.Title = "Chuỗi Streak";
                    vm.Eyebrow = "LIFEHUB · HABIT STREAK";
                    vm.Description = "Theo dõi chuỗi ngày liên tục và kỷ lục duy trì của từng thói quen.";
                    vm.PrimaryActionLabel = "Xem thói quen";
                    vm.Items = Habits();
                    vm.SectionTitle = "Bảng xếp hạng streak";
                    vm.SectionDescription = "Thống kê chuỗi hiện tại và thành tích tốt nhất.";
                    break;
                case "HabitStatistics":
                    vm.Title = "Thống kê thói quen";
                    vm.Eyebrow = "LIFEHUB · HABIT ANALYTICS";
                    vm.Description = "Phân tích tần suất, tỷ lệ hoàn thành và xu hướng duy trì.";
                    vm.PrimaryActionLabel = "Xem báo cáo";
                    vm.Items = Habits();
                    vm.SectionTitle = "Hiệu suất từng thói quen";
                    vm.SectionDescription = "Tỷ lệ hoàn thành minh họa trong 30 ngày gần nhất.";
                    break;
                default: // Goal overview
                    vm.Title = "Tổng quan mục tiêu";
                    vm.Description = "Theo dõi mục tiêu, kết quả then chốt và mức độ hoàn thành.";
                    vm.PrimaryActionLabel = "＋ Thiết lập mục tiêu";
                    vm.SectionTitle = "Mục tiêu trọng tâm";
                    vm.SectionDescription = "Các mục tiêu đang được ưu tiên theo dõi.";
                    break;
            }

            return vm;
        }
    }
}
