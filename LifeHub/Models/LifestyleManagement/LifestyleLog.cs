using System;

namespace LifeHub.Models.LifestyleManagement;

public class LifestyleLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public decimal? SleepHours { get; set; }
    public TimeOnly? BedTime { get; set; }
    public TimeOnly? WakeTime { get; set; }
    public int? SleepQuality { get; set; }
    public int? WaterMl { get; set; }
    public int? PhysicalActivityMinutes { get; set; }
    public string? ActivityType { get; set; }
    public decimal? WeightKg { get; set; }
    public int? MoodRate { get; set; }
    public string? Notes { get; set; }
}
