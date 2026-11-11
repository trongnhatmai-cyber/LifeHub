using System;


namespace LifeHub.Models.ProjectManagement;

public class ProjectActivity
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public string? TargetType { get; set; }

    public int? TargetId { get; set; }

    public DateTime CreatedAt { get; set; }
}

