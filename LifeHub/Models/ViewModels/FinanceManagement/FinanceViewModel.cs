namespace LifeHub.Models.ViewModels.FinanceManagement;

public class FinanceViewModel
{
    // SUMMARY
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalSaving { get; set; }

    // MONTHLY BUDGET
    public decimal MonthlyBudget { get; set; }

    // SAVING GOAL
    public decimal SavingGoal { get; set; }
    public decimal SavingProgress { get; set; }

    // CATEGORY EXPENSE BREAKDOWN
    public List<CategoryViewModel> Categories { get; set; } = new();

    // RECENT TRANSACTIONS
    public List<TransactionViewModel> Transactions { get; set; } = new();
}