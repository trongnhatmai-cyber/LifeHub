namespace LifeHub.Models.ViewModels.FinanceManagement;

public class TransactionViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Category { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }

    public string Type { get; set; } = string.Empty;

     public string Name
    {
        get => Title;
        set => Title = value;
    }

    // Index dùng item.Date
    public DateOnly Date
    {
        get => DateOnly.FromDateTime(TransactionDate);
        set => TransactionDate = value.ToDateTime(TimeOnly.MinValue);
    }
}