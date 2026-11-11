using System;

namespace LifeHub.Models.AIManagement;

public class AiConversation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}
