using System;


namespace LifeHub.Models.FinanceManagement;

public class SavingsGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal? CurrentAmount { get; set; } = 0;
    public DateOnly? Deadline { get; set; }
    public bool IsAchieved { get; set; }
    public DateTime CreatedAt { get; set; }
}
