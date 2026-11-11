using System;


namespace LifeHub.Models.KnowledgeManagement;

public class NoteCategory
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;
}

