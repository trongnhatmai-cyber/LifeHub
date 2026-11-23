using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Tìm kiếm toàn hệ thống (Ctrl + K trên Topbar). Trả JSON chia theo nhóm,
    /// mỗi nhóm tối đa 5 kết quả và chỉ gồm dữ liệu thuộc về người dùng đang đăng nhập.
    /// </summary>
    [Authorize]
    public class SearchController : Controller
    {
        private const int PerGroup = 5;
        private readonly ApplicationDbContext _context;
        public SearchController(ApplicationDbContext context) => _context = context;

        // GET /Search/Query?q=...
        [HttpGet]
        public async Task<IActionResult> Query(string? q)
        {
            q = (q ?? string.Empty).Trim();
            if (q.Length < 2 || q.Length > 80) return Json(new { groups = Array.Empty<object>() });

            var userId = User.GetUserId();
            var groups = new List<object>();

            var tasks = await _context.Tasks.AsNoTracking()
                .Where(t => (t.UserId == userId || t.AssignedTo == userId) && t.Title.Contains(q))
                .OrderByDescending(t => t.CreatedAt).Take(PerGroup)
                .Select(t => new { t.Id, t.Title, t.Status }).ToListAsync();
            if (tasks.Count > 0)
                groups.Add(Group("task", "Công việc", "briefcase",
                    tasks.Select(t => Item(t.Title, t.Status, Url.Action("Task", "Task") ?? "/Task")).ToList()));

            var projectIds = _context.ProjectMembers.AsNoTracking().Where(pm => pm.UserId == userId).Select(pm => pm.ProjectId);
            var projects = await _context.Projects.AsNoTracking()
                .Where(p => projectIds.Contains(p.Id) && p.Name.Contains(q))
                .OrderByDescending(p => p.CreatedAt).Take(PerGroup)
                .Select(p => new { p.Id, p.Name, p.Status }).ToListAsync();
            if (projects.Count > 0)
                groups.Add(Group("project", "Dự án", "users",
                    projects.Select(p => Item(p.Name, "LIFE-" + p.Id, Url.Action("Details", "Project", new { id = p.Id }) ?? "/Project")).ToList()));

            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.UserId == userId && (c.CourseName.Contains(q) || c.CourseCode.Contains(q)))
                .OrderBy(c => c.CourseName).Take(PerGroup)
                .Select(c => new { c.CourseName, c.CourseCode }).ToListAsync();
            if (courses.Count > 0)
                groups.Add(Group("course", "Môn học", "book",
                    courses.Select(c => Item(c.CourseName, c.CourseCode, Url.Action("Courses", "Study") ?? "/Study")).ToList()));

            var notes = await _context.Notes.AsNoTracking()
                .Where(n => n.UserId == userId && n.Title.Contains(q))
                .OrderByDescending(n => n.CreatedAt).Take(PerGroup)
                .Select(n => new { n.Title }).ToListAsync();
            if (notes.Count > 0)
                groups.Add(Group("note", "Ghi chú", "file",
                    notes.Select(n => Item(n.Title, "Ghi chú", Url.Action("Index", "Knowledge") ?? "/Knowledge")).ToList()));

            var goals = await _context.Goals.AsNoTracking()
                .Where(g => g.UserId == userId && g.Title.Contains(q))
                .OrderByDescending(g => g.Id).Take(PerGroup)
                .Select(g => new { g.Title }).ToListAsync();
            if (goals.Count > 0)
                groups.Add(Group("goal", "Mục tiêu", "target",
                    goals.Select(g => Item(g.Title, "Mục tiêu", Url.Action("GoalList", "Goal") ?? "/Goal")).ToList()));

            return Json(new { groups });
        }

        private static object Group(string key, string label, string icon, List<object> items)
            => new { key, label, icon, items };

        private static object Item(string title, string meta, string url) => new { title, meta, url };
    }
}
