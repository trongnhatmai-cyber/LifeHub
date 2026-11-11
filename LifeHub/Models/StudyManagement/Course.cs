using System;


namespace LifeHub.Models.StudyManagement;

public class Course
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? SemesterId { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public int? Credits { get; set; }

    public string? InstructorName { get; set; }

    public string? RoomLocation { get; set; }

    public string? Color { get; set; }

    public decimal? TargetScore { get; set; }

    public decimal? FinalScore { get; set; }

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }
}
