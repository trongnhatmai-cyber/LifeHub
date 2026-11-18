using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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

     public decimal Spent { get; set; }
    public decimal Limit { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Hạn mức không được âm.")]
    public decimal? MonthlyLimit { get; set; }   // Hạn mức chi tiêu mỗi tháng, để trống = không giới hạn

    public decimal SpentThisMonth { get; set; }  // Tổng đã chi tháng này của danh mục (chỉ để hiển thị)
    public IEnumerable<SelectListItem> ParentCategories { get; set; }
        = new List<SelectListItem>();
}