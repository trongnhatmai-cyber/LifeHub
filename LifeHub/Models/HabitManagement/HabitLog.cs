using System;


namespace LifeHub.Models.HabitManagement;

public class HabitLog
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateOnly CheckDate { get; set; }
    public bool IsCompleted { get; set; } = true;
    public int CountValue { get; set; } = 1;
    public string? Note { get; set; }
}
