
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    /// <summary>
    /// Dữ liệu hiển thị trang quản lý danh mục.
    /// </summary>
    public class TaskCategoryViewModel
    {
        public List<TaskCategoryItemViewModel> Categories { get; set; } = new();

        /// <summary>
        /// Tổng số danh mục hiện có.
        /// </summary>
        public int TotalCategories => Categories.Count;

        /// <summary>
        /// Tổng số công việc đã được phân vào danh mục.
        /// </summary>
        public int TotalCategorizedTasks =>
            Categories.Sum(c => c.TaskCount);

        /// <summary>
        /// Số công việc cá nhân chưa có danh mục.
        /// </summary>
        public int UncategorizedTasksCount { get; set; }
    }
}
