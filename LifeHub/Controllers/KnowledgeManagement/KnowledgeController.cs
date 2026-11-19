
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.KnowledgeManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LifeHub.Controllers
{
    [Authorize]
    public class KnowledgeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public KnowledgeController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kiến thức & Tài liệu";
            ViewData["ActivePage"] = "Knowledge";
            var userId = User.GetUserId();
            var folders = await _context.NoteCategories.AsNoTracking().Where(c => c.UserId == userId)
                .Select(c => new KnowledgeCategoryItem { Id=c.Id, Name=c.Name, NoteCount=_context.Notes.Count(n => n.UserId == userId && n.CategoryId == c.Id) }).ToListAsync();
            var notes = await _context.Notes.AsNoTracking().Where(n => n.UserId == userId).OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
                .Select(n => new KnowledgeNoteItem { Id=n.Id, Title=n.Title, Summary=n.Content ?? "", IsPinned=n.IsPinned,
                    UpdatedAt=n.UpdatedAt ?? n.CreatedAt, CategoryName=_context.NoteCategories.Where(c => c.Id == n.CategoryId && c.UserId == userId).Select(c => c.Name).FirstOrDefault() ?? "Chưa phân loại" }).ToListAsync();
            var documents = await _context.Documents.AsNoTracking().Where(d => d.UserId == userId).OrderByDescending(d => d.UploadedAt)
                .Select(d => new KnowledgeDocumentItem { Id=d.Id, FileName=d.FileName, FileType=d.FileType ?? "", UploadedAt=d.UploadedAt,
                    FileSize=d.FileSizeKb.HasValue ? (d.FileSizeKb.Value / 1024m).ToString("0.0", CultureInfo.InvariantCulture) + " MB" : "—",
                    ContextName=d.CourseId.HasValue ? "Khóa học" : d.ProjectId.HasValue ? "Dự án" : "Tài liệu cá nhân" }).ToListAsync();
            return View("~/Views/Knowledge/Index.cshtml", new KnowledgeViewModel { Folders=folders, Notes=notes, Documents=documents,
                TotalNotesCount=notes.Count, PinnedNotesCount=notes.Count(n => n.IsPinned), TotalDocumentsCount=documents.Count,
                StorageUsedText=documents.Sum(d => d.FileSize == "—" ? 0m : decimal.TryParse(d.FileSize.Replace(" MB", ""), CultureInfo.InvariantCulture, out var size) ? size : 0m).ToString("0.0", CultureInfo.InvariantCulture) + " MB" });
        }
    }
}
