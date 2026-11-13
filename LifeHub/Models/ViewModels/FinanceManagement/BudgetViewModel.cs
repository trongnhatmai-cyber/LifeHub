namespace LifeHub.Models.ViewModels.FinanceManagement;

public class BudgetViewModel
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining => Limit - Spent;
}