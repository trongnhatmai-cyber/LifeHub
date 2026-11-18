using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// Namespace riêng để không trùng tên với FinanceViewModel, CategoryViewModel... đã có sẵn.
namespace LifeHub.Models.ViewModels.FinanceModules
{
    /// <summary>Hàm định dạng dùng chung cho các trang Tài chính (vi-VN).</summary>
    public static class FinanceFormat
    {
        public static readonly CultureInfo Vi = new("vi-VN");

        public static string Money(decimal value) => value.ToString("N0", Vi) + " ₫";
        public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", Vi);
        public static string Plain(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        public static string Css(double value) => Math.Round(value, 1).ToString(CultureInfo.InvariantCulture);

        public static string Change(decimal? percent) => !percent.HasValue
            ? "—"
            : (percent.Value >= 0 ? "▲ " : "▼ ") + Math.Abs(Math.Round(percent.Value, 1)).ToString(Vi) + "%";

        public static string MonthLabel(string key) =>
            DateTime.TryParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? $"Tháng {d.Month:00}/{d.Year}" : key;

        /// <summary>Bỏ dấu tiếng Việt và chữ hoa để tìm kiếm ("doan" khớp "Đồ án").</summary>
        public static string Fold(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sb = new StringBuilder();
            foreach (var ch in text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Replace('đ', 'd');
        }
    }

    // ---------------------------------------------------------------- dùng chung
    public class CategoryOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>income / expense</summary>
        public string Type { get; set; } = "expense";
        public string Color { get; set; } = "#e98ba5";
        public decimal? MonthlyLimit { get; set; }
    }

    public class WalletOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>cash / bank / ewallet / savings</summary>
        public string Kind { get; set; } = "cash";
        public decimal Balance { get; set; }
    }

    public class MonthOption
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    // ---------------------------------------------------------------- Giao dịch
    public class TransactionFilter
    {
        /// <summary>all / income / expense</summary>
        public string Type { get; set; } = "all";
        public int? CategoryId { get; set; }
        public int? WalletId { get; set; }
        /// <summary>yyyy-MM, "all", hoặc trống (= tháng hiện tại)</summary>
        public string? Month { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Q { get; set; }

        public bool HasDateRange => From.HasValue || To.HasValue;

        public bool HasAnyFilter => Type != "all" || CategoryId.HasValue || WalletId.HasValue
            || !string.IsNullOrWhiteSpace(Q) || HasDateRange
            || (!string.IsNullOrWhiteSpace(Month) && Month != DateTime.Today.ToString("yyyy-MM"));
    }

    public class TransactionRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>income / expense</summary>
        public string Type { get; set; } = "expense";
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#e98ba5";
        public int WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public bool IsIncome => Type == "income";
        public string AmountLabel => (IsIncome ? "+" : "-") + FinanceFormat.Money(Amount);
    }

    public class TransactionPageViewModel
    {
        public TransactionFilter Filter { get; set; } = new();
        /// <summary>"all" hoặc yyyy-MM, đã áp dụng mặc định.</summary>
        public string EffectiveMonth { get; set; } = "all";
        public string PeriodLabel { get; set; } = string.Empty;
        public List<TransactionRow> Rows { get; set; } = new();
        public List<CategoryOption> Categories { get; set; } = new();
        public List<WalletOption> Wallets { get; set; } = new();
        public List<MonthOption> Months { get; set; } = new();

        // Tổng được tính ở C# trên đúng tập giao dịch đang lọc.
        public decimal TotalIncome => Rows.Where(r => r.IsIncome).Sum(r => r.Amount);
        public decimal TotalExpense => Rows.Where(r => !r.IsIncome).Sum(r => r.Amount);
        public decimal Net => TotalIncome - TotalExpense;
    }

    // ---------------------------------------------------------------- Ví
    public class WalletCard
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "cash";
        public string Color { get; set; } = "#e11d48";
        public decimal Balance { get; set; }
        public decimal MonthIncome { get; set; }
        public decimal MonthExpense { get; set; }
        public List<TransactionRow> Recent { get; set; } = new();

        public bool IsSavings => Kind == "savings";
        public string KindLabel => Kind switch
        {
            "cash" => "Tiền mặt",
            "bank" => "Ngân hàng",
            "ewallet" => "Ví điện tử",
            "savings" => "Quỹ tiết kiệm",
            _ => "Khác"
        };
    }

    public class WalletPageViewModel
    {
        public List<WalletCard> Wallets { get; set; } = new();
        public int WalletCount => Wallets.Count;
        public decimal TotalBalance => Wallets.Sum(w => w.Balance);
        public decimal SavingsBalance => Wallets.Where(w => w.IsSavings).Sum(w => w.Balance);
        public decimal SpendableBalance => TotalBalance - SavingsBalance;
        public decimal MonthIncome => Wallets.Sum(w => w.MonthIncome);
        public decimal MonthExpense => Wallets.Sum(w => w.MonthExpense);
    }

    // ---------------------------------------------------------------- Mục tiêu tiết kiệm
    public class SavingGoalCard
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime? Deadline { get; set; }
        public string Color { get; set; } = "#e11d48";

        public bool IsAchieved => TargetAmount > 0 && CurrentAmount >= TargetAmount;
        public decimal Remaining => Math.Max(0, TargetAmount - CurrentAmount);
        public double Percent => TargetAmount <= 0 ? 0 : Math.Min(100, (double)(CurrentAmount / TargetAmount * 100m));
        public int? DaysLeft => Deadline.HasValue ? (int)(Deadline.Value.Date - DateTime.Today).TotalDays : null;
        public bool IsOverdue => !IsAchieved && DaysLeft.HasValue && DaysLeft.Value < 0;
        public string StateKey => IsAchieved ? "achieved" : IsOverdue ? "overdue" : "active";
        public string StateLabel => IsAchieved ? "Đã hoàn thành" : IsOverdue ? "Quá hạn" : "Đang thực hiện";

        /// <summary>Số tiền nên để dành mỗi tháng để kịp hạn.</summary>
        public decimal? SuggestedMonthly
        {
            get
            {
                if (IsAchieved || !DaysLeft.HasValue || DaysLeft.Value <= 0) return null;
                var months = Math.Max(1, (int)Math.Ceiling(DaysLeft.Value / 30.0));
                return Math.Ceiling(Remaining / months / 1000m) * 1000m;
            }
        }
    }

    public class SavingGoalPageViewModel
    {
        /// <summary>active / achieved / all</summary>
        public string Status { get; set; } = "active";
        public List<SavingGoalCard> Goals { get; set; } = new();
        /// <summary>Các ví có thể nạp tiền vào mục tiêu (không gồm quỹ tiết kiệm).</summary>
        public List<WalletOption> SourceWallets { get; set; } = new();

        // Tổng hợp trên TOÀN BỘ mục tiêu (không phụ thuộc tab đang xem).
        public decimal TotalSaved { get; set; }
        public decimal TotalTarget { get; set; }
        public int ActiveCount { get; set; }
        public int AchievedCount { get; set; }
        public double OverallPercent => TotalTarget <= 0 ? 0 : Math.Min(100, (double)(TotalSaved / TotalTarget * 100m));
    }

    // ---------------------------------------------------------------- Báo cáo
    public class ReportMonthPoint
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public double IncomeHeight { get; set; }
        public double ExpenseHeight { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ReportCategorySlice
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#e98ba5";
        public decimal Amount { get; set; }
        public double Percent { get; set; }
    }

    public class ReportBudgetRow
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#e98ba5";
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public double Percent => Limit <= 0 ? 0 : (double)(Spent / Limit * 100m);
        /// <summary>ok / warn / over</summary>
        public string State => Spent > Limit ? "over" : Percent >= 80 ? "warn" : "ok";
        public decimal Diff => Limit - Spent;
    }

    public class FinanceReportViewModel
    {
        public string Month { get; set; } = string.Empty;
        public string MonthLabel => FinanceFormat.MonthLabel(Month);
        public string? PrevMonth { get; set; }
        public string? NextMonth { get; set; }

        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net => Income - Expense;
        public decimal SavingsRate => Income <= 0 ? 0 : Math.Round(Net / Income * 100m, 1);
        public decimal? IncomeChange { get; set; }
        public decimal? ExpenseChange { get; set; }

        public List<ReportMonthPoint> Trend { get; set; } = new();
        public List<ReportCategorySlice> Slices { get; set; } = new();
        public string DonutGradient { get; set; } = "conic-gradient(var(--finance-soft) 0% 100%)";
        public List<ReportBudgetRow> BudgetRows { get; set; } = new();
        public decimal TotalLimit => BudgetRows.Sum(b => b.Limit);
        public decimal TotalBudgetSpent => BudgetRows.Sum(b => b.Spent);
        public List<TransactionRow> TopExpenses { get; set; } = new();
        public List<string> Insights { get; set; } = new();
    }
}