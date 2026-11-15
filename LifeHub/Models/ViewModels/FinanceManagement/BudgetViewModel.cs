
namespace LifeHub.Models.ViewModels.FinanceManagement
{
    public class BudgetViewModel
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public string Category { get; set; } = string.Empty;

        public decimal Limit { get; set; }

        public decimal Spent { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Remaining => Limit - Spent;

        public decimal UsagePercent => Limit > 0 ? Spent / Limit * 100 : 0;
    }
}
