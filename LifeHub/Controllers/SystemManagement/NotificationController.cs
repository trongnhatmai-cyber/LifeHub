using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.SystemManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Trang Thông báo (/Notification) và dữ liệu cho dropdown chuông trên Topbar (/Notification/Recent).
    /// Chỉ thao tác trên thông báo của chính người dùng đang đăng nhập.
    /// </summary>
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NotificationController(ApplicationDbContext context) => _context = context;

        // GET /Notification
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Thông báo";
            ViewData["ActivePage"] = "Notification";

            var userId = User.GetUserId();
            var items = await _context.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(200)
                .Select(n => new NotificationItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    LinkUrl = n.LinkUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return View(new NotificationPageViewModel { Items = items });
        }

        // GET /Notification/Recent  -> JSON cho dropdown Topbar: số chưa đọc + 5 thông báo gần nhất
        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = User.GetUserId();
            var unread = await _context.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);
            var rows = await _context.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationItem
                {
                    Id = n.Id, Title = n.Title, Message = n.Message, NotificationType = n.NotificationType,
                    LinkUrl = n.LinkUrl, IsRead = n.IsRead, CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            var now = DateTime.Now;
            return Json(new
            {
                unread,
                items = rows.Select(n => new
                {
                    id = n.Id, title = n.Title, message = n.Message, time = n.TimeAgo(now),
                    unread = !n.IsRead, icon = n.Icon, tone = n.Tone, url = n.SafeLink
                })
            });
        }

        // POST /Notification/MarkRead
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.GetUserId();
            await _context.Notifications.Where(n => n.Id == id && n.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return Done();
        }

        // POST /Notification/MarkAllRead
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.GetUserId();
            await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return Done();
        }

        // POST /Notification/Delete
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            await _context.Notifications.Where(n => n.Id == id && n.UserId == userId).ExecuteDeleteAsync();
            return Done();
        }

        // Gọi bằng fetch (JS) thì trả 200; gửi form thường thì quay lại trang Thông báo.
        private IActionResult Done()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Index));
        }
    }
}
