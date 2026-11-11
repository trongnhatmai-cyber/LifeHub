using System;


namespace LifeHub.Models.HabitManagement;

public class Habit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int TargetPerPeriod { get; set; } = 1;
    public TimeOnly? ReminderTime { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}