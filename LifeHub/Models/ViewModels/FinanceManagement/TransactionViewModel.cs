
namespace LifeHub.Models.ViewModels.FinanceManagement;

public class TransactionViewModel
{
    // Thống kê tổng quan
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }

    public decimal Net => TotalIncome - TotalExpense;

    // Thời gian đang xem
    public string EffectiveMonth { get; set; } = "all";
    public string PeriodLabel { get; set; } = "Tất cả thời gian";

    // Bộ lọc
    public TransactionFilterViewModel Filter { get; set; } = new();

    // Danh sách tháng
    public List<TransactionMonthViewModel> Months { get; set; } = new();

    // Danh sách danh mục
    public List<TransactionCategoryViewModel> Categories { get; set; } = new();

    // Danh sách ví
    public List<TransactionWalletViewModel> Wallets { get; set; } = new();

    // Danh sách giao dịch
    public List<TransactionRowViewModel> Rows { get; set; } = new();
}

// Bộ lọc giao dịch
public class TransactionFilterViewModel
{
    public string Type { get; set; } = "all";

    public int? CategoryId { get; set; }

    public int? WalletId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public string? Q { get; set; }
}

// Thông tin tháng dùng cho bộ lọc
public class TransactionMonthViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

// Danh mục dùng cho bộ lọc
public class TransactionCategoryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

// Ví dùng cho bộ lọc
public class TransactionWalletViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

// Một dòng giao dịch trong bảng
public class TransactionRowViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string? CategoryColor { get; set; }

    public string WalletName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    public bool IsIncome { get; set; }

    public string AmountLabel { get; set; } = string.Empty;
}
