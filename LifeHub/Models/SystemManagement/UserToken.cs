using System;


namespace LifeHub.Models.SystemManagement;

public class UserToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenType { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
