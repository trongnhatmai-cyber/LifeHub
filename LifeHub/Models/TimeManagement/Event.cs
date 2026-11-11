using System;


namespace LifeHub.Models.TimeManagement;

public class Event
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateOnly EndTime { get; set; }

    public bool IsAllDay { get; set; } = false;

    public string? Color { get; set; }

    public string? RecurrenceRule { get; set; }

    public string? SourceType { get; set; }

    public int? SourceId { get; set; }

    public DateTime CreatedAt { get; set; }
}

