using LifeHub.Models.ViewModels;

namespace LifeHub.Data.Mock
{
    /// <summary>
    /// Mock data của Dashboard. Sau này thay bằng service đọc Database
    /// (ví dụ IDashboardService) và giữ nguyên DashboardViewModel.
    /// </summary>
    public static class MockDashboardData
    {
        public static DashboardViewModel Build(string userName, string todayFormatted)
        {
            var model = new DashboardViewModel
            {
                UserName = userName,
                TodayDateFormatted = todayFormatted,
                WeeklyMomentumPercent = 68,
                ActiveProjectsCount = 3,
                CurrentGpa = 3.42m,
                TotalBalance = 12_200_000m,

                TodayTasks = new List<DashboardTaskItem>
                {
                    new() { Id = 1, Title = "Nộp bài tập Cơ sở dữ liệu", Priority = "Cao", Category = "Học tập", DueTime = "17:00 hôm nay", IsCompleted = false },
                    new() { Id = 2, Title = "Họp nhóm đồ án LifeHub", Priority = "Trung bình", Category = "Dự án", DueTime = "19:30 hôm nay", IsCompleted = false },
                    new() { Id = 3, Title = "Viết báo cáo tuần", Priority = "Thấp", Category = "Công việc", DueTime = "Cuối ngày", IsCompleted = false },
                    new() { Id = 4, Title = "Ôn từ vựng IELTS", Priority = "Thấp", Category = "Học tập", DueTime = "Sáng nay", IsCompleted = true }
                },

                UpcomingEvents = new List<DashboardEventItem>
                {
                    new() { Id = 1, Title = "Lập trình Web ASP.NET", Time = "07:30", Location = "Phòng A2.04", Tag = "Lịch học" },
                    new() { Id = 2, Title = "Họp nhóm đồ án", Time = "19:30", Location = "Google Meet", Tag = "Dự án" },
                    new() { Id = 3, Title = "Thi giữa kỳ Cơ sở dữ liệu", Time = "Thứ 5, 08:00", Location = "Phòng B1.12", Tag = "Lịch thi" }
                },

                Goals = new List<DashboardGoalItem>
                {
                    new() { Id = 1, Title = "IELTS 7.0", ProgressPercent = 40, Category = "Học tập" },
                    new() { Id = 2, Title = "Mua laptop mới", ProgressPercent = 61, Category = "Tài chính" },
                    new() { Id = 3, Title = "Chạy bộ 100 km", ProgressPercent = 27, Category = "Sức khỏe" }
                },

                Habits = new List<DashboardHabitItem>
                {
                    new() { Id = 1, Title = "Chạy bộ", StreakDays = 6, IsCompletedToday = false },
                    new() { Id = 2, Title = "Đọc sách 20 phút", StreakDays = 12, IsCompletedToday = true },
                    new() { Id = 3, Title = "Uống đủ 8 ly nước", StreakDays = 3, IsCompletedToday = false }
                }
            };

            // Tính từ danh sách để số trên thẻ luôn khớp với danh sách bên dưới.
            model.PendingTasksCount = model.TodayTasks.Count(t => !t.IsCompleted);
            return model;
        }
    }
}