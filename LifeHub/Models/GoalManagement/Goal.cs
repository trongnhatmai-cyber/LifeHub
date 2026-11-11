using System;


namespace LifeHub.Models.GoalManagement;

public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string GoalType { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "active";
    public int ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
}