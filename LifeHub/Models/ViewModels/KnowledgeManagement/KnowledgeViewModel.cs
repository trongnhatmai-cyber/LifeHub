using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.KnowledgeManagement
{
    public class KnowledgeViewModel
    {
        public int TotalNotesCount { get; set; } = 15;
        public int TotalDocumentsCount { get; set; } = 8;
        public int PinnedNotesCount { get; set; } = 3;
        public string StorageUsedText { get; set; } = "42.5 MB";

        public List<KnowledgeCategoryItem> Folders { get; set; } = new();
        public List<KnowledgeNoteItem> Notes { get; set; } = new();
        public List<KnowledgeDocumentItem> Documents { get; set; } = new();
    }

    public class KnowledgeCategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int NoteCount { get; set; }
    }

    public class KnowledgeNoteItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "Học tập";
        public bool IsPinned { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Tag { get; set; } = string.Empty;
    }

    public class KnowledgeDocumentItem
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = "PDF"; // PDF, DOCX, ZIP, PNG
        public string FileSize { get; set; } = "2.4 MB";
        public string ContextName { get; set; } = "Môn C# (NET108)";
        public DateTime UploadedAt { get; set; }
    }
}
