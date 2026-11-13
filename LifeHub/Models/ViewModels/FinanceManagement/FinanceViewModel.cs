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

        public List<FinanceCategoryViewModel> Categories { get; set; } = new();
        public List<FinanceTransactionViewModel> Transactions { get; set; } = new();
    }

    public class FinanceCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Spent { get; set; }
        public decimal Limit { get; set; }
    }

    public class FinanceTransactionViewModel
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}