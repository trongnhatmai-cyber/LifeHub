using System;


namespace LifeHub.Models.ProjectManagement;

public class ProjectRole
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}
