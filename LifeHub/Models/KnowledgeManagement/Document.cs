using System;


namespace LifeHub.Models.KnowledgeManagement;

public class Document
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ProjectId { get; set; }

    public int? CourseId { get; set; }

    public int? TaskId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? FileType { get; set; }

    public int? FileSizeKb { get; set; }

    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; }
}
