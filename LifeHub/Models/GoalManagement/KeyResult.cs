using System;


namespace LifeHub.Models.GoalManagement;

public class KeyResult
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? CurrentValue { get; set; } = 0;
    public string? Unit { get; set; }
    public bool IsCompleted { get; set; }
}
