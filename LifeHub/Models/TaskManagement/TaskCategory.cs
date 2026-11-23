namespace LifeHub.Models.TaskManagement;

public class TaskCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#8B9CF6";
    public string Icon { get; set; } = "folder";
}
