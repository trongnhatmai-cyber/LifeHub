
using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.FinanceManagement
{
    public class FinanceViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalSaving { get; set; }

        public decimal MonthlyBudget { get; set; }
        public decimal SavingGoal { get; set; }
        public decimal SavingProgress { get; set; }

        // Ngân sách tháng và ngân sách từng danh mục
        public MonthlyBudgetViewModel MonthlyBudgetDetails { get; set; } = new();

        // Danh sách mục tiêu tiết kiệm
        public List<SavingGoalViewModel> SavingGoals { get; set; } = new();

        // Dữ liệu biểu đồ dòng tiền
        public CashFlowViewModel CashFlow { get; set; } = new();

        // Danh sách danh mục
        public List<FinanceCategoryViewModel> Categories { get; set; } = new();

        // Danh sách giao dịch
        public List<FinanceTransactionViewModel> Transactions { get; set; } = new();

        public TransactionCreateViewModel CreateTransaction { get; set; } = new();
    }

    public class FinanceCategoryViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public decimal Spent { get; set; }

        public decimal Limit { get; set; }
    }

    public class FinanceTransactionViewModel
    {
        public int Id { get; set; }

        public int? CategoryId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }
    }
}
