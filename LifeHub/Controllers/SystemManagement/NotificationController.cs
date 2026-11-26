
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.SystemManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: /Notification
    // Danh sách thông báo + thông tin lời mời dự án
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();

        var userEmail = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync() ?? string.Empty;

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(200)
            .ToListAsync();

        // Lấy các lời mời dự án liên quan đến thông báo
        var invitationIds = notifications
            .Where(n => n.RefType == "project_invitation" && n.RefId.HasValue)
            .Select(n => n.RefId!.Value)
            .Distinct()
            .ToArray();

        var invitations = await _context.ProjectInvitations
            .AsNoTracking()
            .Where(i => invitationIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        // Lấy tên dự án
        var projectIds = invitations.Values
            .Select(i => i.ProjectId)
            .Distinct()
            .ToArray();

        var projectNames = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var model = new NotificationPageViewModel
        {
            Items = notifications.Select(n =>
            {
                invitations.TryGetValue(n.RefId ?? 0, out var invitation);

                var projectId = invitation?.ProjectId ?? 0;

                // Kiểm tra người nhận lời mời
                var isRecipient = invitation != null &&
                    (
                        invitation.InvitedUserId == userId ||
                        (
                            invitation.InvitedUserId == null &&
                            string.Equals(
                                invitation.InvitedEmail,
                                userEmail,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    );

                // Chỉ cho phép thao tác khi lời mời còn hiệu lực
                var actionable =
                    invitation != null &&
                    isRecipient &&
                    invitation.Status == "pending" &&
                    (
                        !invitation.ExpiresAt.HasValue ||
                        invitation.ExpiresAt.Value > DateTime.Now
                    );

                return new NotificationItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    LinkUrl = n.LinkUrl,
                    NotificationType = n.NotificationType,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,

                    ProjectId = projectId,

                    ProjectName = projectNames.GetValueOrDefault(
                        projectId,
                        "Dự án"
                    ),

                    InvitationId = actionable
                        ? invitation!.Id
                        : null,

                    InvitationStatus = invitation?.Status
                };
            }).ToList()
        };

        ViewData["Title"] = "Thông báo";
        ViewData["ActivePage"] = "Notification";

        return View(model);
    }

    // =========================================================
    // GET: /Notification/Recent
    // Dữ liệu JSON cho dropdown chuông Topbar
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var userId = User.GetUserId();

        var unread = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n =>
                n.UserId == userId &&
                !n.IsRead
            );

        var rows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
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

        var now = DateTime.Now;

        return Json(new
        {
            unread,

            items = rows.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                time = n.TimeAgo(now),
                unread = !n.IsRead,
                icon = n.Icon,
                tone = n.Tone,
                url = n.SafeLink
            })
        });
    }

    // =========================================================
    // POST: /Notification/MarkRead
    // Đánh dấu một thông báo đã đọc
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.GetUserId();

        await _context.Notifications
            .Where(n =>
                n.Id == id &&
                n.UserId == userId
            )
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(n => n.IsRead, true)
            );

        return Done();
    }

    // =========================================================
    // POST: /Notification/MarkAllRead
    // Đánh dấu tất cả thông báo đã đọc
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();

        await _context.Notifications
            .Where(n =>
                n.UserId == userId &&
                !n.IsRead
            )
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(n => n.IsRead, true)
            );

        return Done();
    }

    // =========================================================
    // POST: /Notification/Delete
    // Xóa thông báo của người dùng hiện tại
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();

        await _context.Notifications
            .Where(n =>
                n.Id == id &&
                n.UserId == userId
            )
            .ExecuteDeleteAsync();

        return Done();
    }

    // =========================================================
    // Xử lý kết quả POST
    // AJAX: trả về HTTP 200
    // Form thông thường: quay lại trang thông báo
    // =========================================================

    private IActionResult Done()
    {
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction(nameof(Index));
    }
}
