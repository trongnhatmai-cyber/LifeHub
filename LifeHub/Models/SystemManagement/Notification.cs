using System;


namespace LifeHub.Models.SystemManagement;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string? RefType { get; set; }
    public int? RefId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}
