using System;


namespace LifeHub.Models.SystemManagement;

public class Reminder
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string RefType { get; set; } = string.Empty;

    public int RefId { get; set; }

    public DateTime RemindAt { get; set; }

    public int? OffsetMinutes { get; set; }

    public bool IsSent { get; set; } = false;

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
