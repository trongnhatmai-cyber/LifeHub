
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.FinanceManagement;

public class TransactionCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn loại giao dịch.")]
    [RegularExpression(
        "^(Income|Expense|Transfer)$",
        ErrorMessage = "Loại giao dịch không hợp lệ."
    )]
    public string TransactionType { get; set; } = "Expense";

    [Required(ErrorMessage = "Vui lòng chọn ví.")]
    [Range(1, int.MaxValue, ErrorMessage = "Ví không hợp lệ.")]
    public int WalletId { get; set; }

    public int? ToWalletId { get; set; }

    public int? CategoryId { get; set; }

    public int? SavingsGoalId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền.")]
    [Range(
        typeof(decimal),
        "0.01",
        "999999999999",
        ErrorMessage = "Số tiền phải lớn hơn 0."
    )]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày giao dịch.")]
    public DateOnly TransactionDate { get; set; }
        = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }
}
