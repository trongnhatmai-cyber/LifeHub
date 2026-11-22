
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    /// <summary>
    /// Dữ liệu nhập từ modal thêm hoặc chỉnh sửa danh mục.
    /// </summary>
    public class TaskCategoryInputViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string Color { get; set; } = "#8B9CF6";

        [Required]
        public string Icon { get; set; } = "folder";
    }
}
