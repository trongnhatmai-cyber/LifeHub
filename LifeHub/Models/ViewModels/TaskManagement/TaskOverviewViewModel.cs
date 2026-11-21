
using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    /// <summary>
    /// Dữ liệu hiển thị trang Tổng quan công việc.
    /// </summary>
    public class TaskOverviewViewModel
    {
        public List<TaskItemViewModel> Tasks { get; set; } = new();
        public List<TaskProjectOption> Projects { get; set; } = new();
        public List<TaskCategoryOption> Categories { get; set; } = new();

        /// <summary>
        /// Tổng số công việc.
        /// </summary>
        public int TotalTasks => Tasks.Count;

        public int InProgressTasksCount =>
            Tasks.Count(t => t.Status == "in_progress");

        public int CompletedTasksCount =>
            Tasks.Count(t => t.Status == "done");

        public int OverdueTasksCount =>
            Tasks.Count(t => t.IsOverdue);

        /// <summary>
        /// Công việc cá nhân không bao gồm việc được người khác giao.
        /// </summary>
        public int PersonalTasksCount =>
            Tasks.Count(t => !t.IsProjectTask && !t.IsAssignedToMe);

        public int AssignedToMeCount =>
            Tasks.Count(t => t.IsAssignedToMe);

        public int ProjectTasksCount =>
            Tasks.Count(t => t.IsProjectTask);

        /// <summary>
        /// Phần trăm công việc đã hoàn thành.
        /// </summary>
        public int CompletionPercent =>
            TotalTasks == 0
                ? 0
                : (int)Math.Round(
                    CompletedTasksCount * 100.0 / TotalTasks);

        /// <summary>
        /// Tỷ lệ công việc hoàn thành đúng hạn.
        /// Chỉ tính các công việc đã hoàn thành và có Deadline.
        /// </summary>
        public int? OnTimeCompletionRate
        {
            get
            {
                var completedTasks = Tasks
                    .Where(t => t.IsDone
                        && t.Deadline.HasValue
                        && t.CompletedAt.HasValue)
                    .ToList();

                if (!completedTasks.Any())
                    return null;

                var onTimeTasks = completedTasks.Count(t =>
                    t.CompletedAt!.Value.Date <= t.Deadline!.Value.Date);

                return (int)Math.Round(
                    onTimeTasks * 100.0 / completedTasks.Count);
            }
        }

        /// <summary>
        /// Sáu công việc được tạo gần đây nhất.
        /// </summary>
        public List<TaskItemViewModel> RecentTasks =>
            Tasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(6)
                .ToList();

        /// <summary>
        /// Công việc quá hạn, sắp đến hạn hoặc có ưu tiên cao.
        /// </summary>
        public List<TaskItemViewModel> AttentionTasks =>
            Tasks
                .Where(t =>
                    t.IsOverdue ||
                    t.IsDueSoon ||
                    (!t.IsDone &&
                     (t.Priority == "urgent" || t.Priority == "high")))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .Take(6)
                .ToList();

        /// <summary>
        /// Dữ liệu biểu đồ số công việc hoàn thành trong 7 ngày gần nhất.
        /// </summary>
        public List<TaskCompletionTrendItem> CompletionTrend
        {
            get
            {
                return Enumerable.Range(0, 7)
                    .Select(offset =>
                    {
                        var date = DateTime.Today.AddDays(offset - 6);

                        return new TaskCompletionTrendItem
                        {
                            Date = date,
                            CompletedCount = Tasks.Count(t =>
                                t.CompletedAt.HasValue &&
                                t.CompletedAt.Value.Date == date.Date)
                        };
                    })
                    .ToList();
            }
        }
    }
}
