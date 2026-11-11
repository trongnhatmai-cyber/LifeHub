using System;


namespace LifeHub.Models.ProjectManagement;

public class ProjectMilestone
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    public bool IsCompleted { get; set; } = false;
}
