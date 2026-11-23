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

    public NotificationController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var userEmail = await _context.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => u.Email).FirstOrDefaultAsync() ?? string.Empty;
        var notifications = await _context.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        var invitationIds = notifications.Where(n => n.RefType == "project_invitation" && n.RefId.HasValue)
            .Select(n => n.RefId!.Value).Distinct().ToArray();
        var invitations = await _context.ProjectInvitations.AsNoTracking()
            .Where(i => invitationIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var projectIds = invitations.Values.Select(i => i.ProjectId).Distinct().ToArray();
        var projectNames = await _context.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var model = new NotificationPageViewModel
        {
            Items = notifications.Select(n =>
            {
                invitations.TryGetValue(n.RefId ?? 0, out var invitation);
                var projectId = invitation?.ProjectId ?? 0;
                var isRecipient = invitation != null &&
                    (invitation.InvitedUserId == userId || invitation.InvitedUserId == null &&
                     invitation.InvitedEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
                var actionable = invitation != null && isRecipient && invitation.Status == "pending" &&
                    (!invitation.ExpiresAt.HasValue || invitation.ExpiresAt > DateTime.Now);
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
                    ProjectName = projectNames.GetValueOrDefault(projectId, "Dự án"),
                    InvitationId = actionable ? invitation!.Id : null,
                    InvitationStatus = invitation?.Status
                };
            }).ToList()
        };

        ViewData["Title"] = "Thông báo";
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.GetUserId();
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();
        await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
        return RedirectToAction(nameof(Index));
    }
}
