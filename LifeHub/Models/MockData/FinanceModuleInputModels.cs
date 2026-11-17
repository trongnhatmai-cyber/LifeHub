using System;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.FinanceModules
{
    // Dữ liệu form gửi lên Controller. Tên thuộc tính trùng với name="" của input trong các View.

    /// <summary>Thêm hoặc sửa giao dịch (bảng transactions). Id = 0 khi thêm mới.</summary>
    public class SaveTransactionInput
    {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
        /// <summary>income / expense</summary>
        [Required] public string Type { get; set; } = "expense";
        [Required] public int CategoryId { get; set; }
        [Required] public int WalletId { get; set; }
        [Range(1000, 1_000_000_000)] public decimal Amount { get; set; }
        [Required] public DateTime Date { get; set; }
    }

    /// <summary>Thêm hoặc sửa ví (bảng wallets).</summary>
    public class SaveWalletInput
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        /// <summary>cash / bank / ewallet</summary>
        [Required] public string Kind { get; set; } = "cash";
        public decimal OpeningBalance { get; set; }
        public string Color { get; set; } = "#e11d48";
    }

    /// <summary>Chuyển tiền giữa hai ví (sinh giao dịch loại chuyển ví).</summary>
    public class TransferInput
    {
        [Required] public int FromWalletId { get; set; }
        [Required] public int ToWalletId { get; set; }
        [Range(1000, 1_000_000_000)] public decimal Amount { get; set; }
        [StringLength(200)] public string? Note { get; set; }
    }

    /// <summary>Thêm hoặc sửa mục tiêu tiết kiệm (bảng savings_goals).</summary>
    public class SaveSavingGoalInput
    {
        public int Id { get; set; }
        [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
        [Range(1000, 10_000_000_000)] public decimal TargetAmount { get; set; }
        /// <summary>Số tiền đã có sẵn khi tạo mục tiêu (chỉ dùng khi thêm mới).</summary>
        [Range(0, 10_000_000_000)] public decimal CurrentAmount { get; set; }
        public DateTime? Deadline { get; set; }
        public string Color { get; set; } = "#e11d48";
    }

    /// <summary>Nạp tiền vào mục tiêu: chuyển từ ví nguồn sang quỹ tiết kiệm, gắn savings_goal_id.</summary>
    public class ContributeInput
    {
        [Required] public int GoalId { get; set; }
        [Required] public int WalletId { get; set; }
        [Range(1000, 1_000_000_000)] public decimal Amount { get; set; }
    }
}