using System.Collections.Generic;

//Tổng ngân sách tháng
namespace LifeHub.Models.ViewModels.FinanceManagement
{
    public class MonthlyBudgetViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }

        // Tổng số tiền được phép sử dụng trong tháng
        public decimal Limit { get; set; }

        // Tổng số tiền đã chi trong tháng
        public decimal Spent { get; set; }

        public decimal Remaining => Limit - Spent;

        public decimal UsagePercent =>
            Limit > 0 ? Spent / Limit * 100 : 0;

        public List<BudgetViewModel> CategoryBudgets { get; set; } = new();
    }
}