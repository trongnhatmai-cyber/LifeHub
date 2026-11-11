using System;

namespace LifeHub.Models.KnowledgeManagement;

public class SharedItem
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int SharedWithUserId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string Permission { get; set; } = "view";
    public DateTime CreatedAt { get; set; }
}