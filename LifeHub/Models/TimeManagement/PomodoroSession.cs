using System;


namespace LifeHub.Models.TimeManagement;

public class PomodoroSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? TaskId { get; set; }

    public int? CourseId { get; set; }

    public int DurationMinutes { get; set; }

    public int? BreakMinutes { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted { get; set; } = false;
}

