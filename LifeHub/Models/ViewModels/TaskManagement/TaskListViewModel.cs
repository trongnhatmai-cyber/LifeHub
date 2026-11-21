
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    /// <summary>
    /// Dữ liệu hiển thị trang Danh sách công việc.
    /// </summary>
    public class TaskListViewModel
    {
        /// <summary>
        /// Danh sách công việc hiển thị trên Kanban.
        /// </summary>
        public List<TaskItemViewModel> Tasks { get; set; } = new();

        /// <summary>
        /// Các dự án người dùng có thể chọn.
        /// </summary>
        public List<TaskProjectOption> Projects { get; set; } = new();

        /// <summary>
        /// Các danh mục dùng trong bộ lọc và form.
        /// </summary>
        public List<TaskCategoryOption> Categories { get; set; } = new();

        public int TotalTasks => Tasks.Count;

        public int TodoTasksCount =>
            Tasks.Count(t => t.Status == "todo");

        public int InProgressTasksCount =>
            Tasks.Count(t => t.Status == "in_progress");

        public int ReviewTasksCount =>
            Tasks.Count(t => t.Status == "review");

        public int CompletedTasksCount =>
            Tasks.Count(t => t.Status == "done");

        public int OverdueTasksCount =>
            Tasks.Count(t => t.IsOverdue);

        public int PersonalTasksCount =>
            Tasks.Count(t => !t.IsProjectTask && !t.IsAssignedToMe);

        public int AssignedToMeCount =>
            Tasks.Count(t => t.IsAssignedToMe);

        public int ProjectTasksCount =>
            Tasks.Count(t => t.IsProjectTask);
    }
}
