using System;


namespace LifeHub.Models.TaskManagement;

public class Task
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? AssignedTo { get; set; }

    public int? ProjectId { get; set; }

    public int? ParentTaskId { get; set; }

    public int? GoalId { get; set; }

    public int? CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Priority { get; set; } = "medium";

    public string Status { get; set; } = "todo";

    public DateTime? Deadline { get; set; }

    public int? EstimatedMinutes { get; set; }

    public string? RecurrenceRule { get; set; }

    public int SortOrder { get; set; } = 0;

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

