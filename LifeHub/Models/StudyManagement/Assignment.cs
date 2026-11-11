using System;


namespace LifeHub.Models.StudyManagement;

public class Assignment
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AssignmentType { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Location { get; set; }

    public int WeightPercent { get; set; } = 0;

    public decimal? Score { get; set; }

    public decimal? MaxScore { get; set; } = 10;

    public bool IsSubmitted { get; set; } = false;

    public string? Notes { get; set; }
}
