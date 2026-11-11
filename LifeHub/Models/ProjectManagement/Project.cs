using System;


namespace LifeHub.Models.ProjectManagement;

public class Project
{
    public int Id { get; set; }

    public int CreatedBy { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = "planning";

    public int ProgressPercent { get; set; } = 0;

    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
