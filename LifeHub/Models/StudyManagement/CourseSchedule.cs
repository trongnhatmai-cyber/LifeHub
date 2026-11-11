using System;


namespace LifeHub.Models.StudyManagement;

public class CourseSchedule
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string? RoomOrLink { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
