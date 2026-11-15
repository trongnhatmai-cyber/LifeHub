using System;

namespace LifeHub.Models.ViewModels.FinanceManagement
{
    public class SavingGoalViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; }

        public DateTime? Deadline { get; set; }

        public decimal RemainingAmount =>
            Math.Max(0, TargetAmount - CurrentAmount);

        public decimal ProgressPercent =>
            TargetAmount > 0
                ? Math.Min(100, CurrentAmount / TargetAmount * 100)
                : 0;
    }
}