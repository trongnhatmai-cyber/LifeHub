using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.FinanceManagement;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại danh mục.")]
    public string Type { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Color { get; set; }

    public int? ParentId { get; set; }
}