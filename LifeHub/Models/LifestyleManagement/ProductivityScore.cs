using System;

namespace LifeHub.Models.LifestyleManagement;

public class ProductivityScore
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly ScoreDate { get; set; }
    public int ScoreValue { get; set; }
    public int? TasksCompleted { get; set; }
    public int? HabitsCompleted { get; set; }
    public int? PomodorosDone { get; set; }
    public int? OverduePenalties { get; set; }
    public DateTime CreatedAt { get; set; }
}
