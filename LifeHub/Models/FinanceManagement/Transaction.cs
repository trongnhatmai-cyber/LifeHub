using System;


namespace LifeHub.Models.FinanceManagement;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int WalletId { get; set; }
    public int? ToWalletId { get; set; }
    public int? CategoryId { get; set; }
    public int? SavingsGoalId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
