
using LifeHub.DataAccessLayer;
using LifeHub.Models;
using LifeHub.Models.SystemManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace LifeHub.BusinessLogicLayer.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    // Đăng ký tài khoản
    public async Task<bool> RegisterAsync(
        string email,
        string password,
        string fullName)
    {
        email = email.Trim().ToLowerInvariant();
        fullName = fullName.Trim();
        if (email.Length > 254 || fullName.Length is < 1 or > 150 || string.IsNullOrWhiteSpace(password))
            return false;

        // Kiểm tra email đã tồn tại
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
            return false;

        var user = new User
        {
            Email = email,
            FullName = fullName.Trim(),
            Role = UserRole.User,
            Active = true,
            EmailVerified = false
        };

        // Băm mật khẩu trước khi lưu
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password
        );

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }

        // Lời mời gửi tới email chưa đăng ký được liên kết và thông báo ngay khi tài khoản được tạo.
        var pendingInvitations = await _context.ProjectInvitations
            .Where(i => i.InvitedUserId == null && i.InvitedEmail == email && i.Status == "pending" &&
                        (!i.ExpiresAt.HasValue || i.ExpiresAt > DateTime.Now))
            .ToListAsync();
        if (pendingInvitations.Count > 0)
        {
            var projectIds = pendingInvitations.Select(i => i.ProjectId).Distinct().ToArray();
            var projectNames = await _context.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);
            var inviterIds = pendingInvitations.Select(i => i.InvitedBy).Distinct().ToArray();
            var inviterNames = await _context.Users.AsNoTracking().Where(u => inviterIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            foreach (var invitation in pendingInvitations)
            {
                invitation.InvitedUserId = user.Id;
                var inviterName = inviterNames.GetValueOrDefault(invitation.InvitedBy, "Một thành viên");
                var projectName = projectNames.GetValueOrDefault(invitation.ProjectId, "dự án");
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Lời mời tham gia dự án",
                    Message = $"{inviterName} đã mời bạn tham gia dự án “{projectName}”.",
                    LinkUrl = $"/Project/Details/{invitation.ProjectId}",
                    NotificationType = "project_invitation",
                    RefType = "project_invitation",
                    RefId = invitation.Id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        return true;
    }

    // Kiểm tra thông tin đăng nhập
    public async Task<User?> ValidateUserAsync(
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !user.Active)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        if (result == PasswordVerificationResult.Failed)
            return null;

        // Cập nhật thời gian đăng nhập
        user.LastLoginAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return user;
    }
}
