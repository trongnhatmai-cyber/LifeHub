using System.Security.Claims;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models;
using LifeHub.Models.KnowledgeManagement;
using LifeHub.Models.SystemManagement;
using LifeHub.Models.ViewModels.SystemManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    /// <summary>
    /// Cài đặt chung: Tổng quan, Tài khoản, Giao diện, Thông báo, Bảo mật, Chia sẻ, Nhật ký, Thành tựu, Trợ giúp.
    /// Mọi truy vấn đều lọc theo người dùng đang đăng nhập; quyền được kiểm tra ở đây, không dựa vào việc ẩn nút trên View.
    /// </summary>
    [Authorize]
    public class SettingsController : Controller
    {
        private static readonly string[] Timezones =
        {
            "Asia/Ho_Chi_Minh", "Asia/Bangkok", "Asia/Singapore", "Asia/Tokyo", "Asia/Seoul",
            "Europe/London", "Europe/Paris", "America/New_York", "America/Los_Angeles", "Australia/Sydney"
        };

        private readonly ApplicationDbContext _context;
        public SettingsController(ApplicationDbContext context) => _context = context;

        // ---------------------------------------------------------------- helpers
        private void Page(string title)
        {
            ViewData["Title"] = title;
            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Settings";
        }

        private async Task<UserSettings> GetSettingsAsync(int userId)
        {
            var s = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
            if (s == null)
            {
                s = new UserSettings { UserId = userId };
                _context.UserSettings.Add(s);
                await _context.SaveChangesAsync();
            }
            return s;
        }

        private void Log(int userId, string action, string details)
        {
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });
        }

        private IActionResult Back(string action, string? ok = null, string? error = null)
        {
            if (ok != null) TempData["SuccessMessage"] = ok;
            if (error != null) TempData["ErrorMessage"] = error;
            return RedirectToAction(action);
        }

        // ---------------------------------------------------------------- 1. Tổng quan
        [HttpGet]
        public IActionResult Index()
        {
            Page("Cài đặt");
            return View();
        }

        // ---------------------------------------------------------------- 2. Tài khoản cá nhân
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            Page("Tài khoản cá nhân");
            return View(await BuildProfileAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileInput input)
        {
            if (!ModelState.IsValid) return Back(nameof(Profile), error: "Thông tin chưa hợp lệ. Họ tên không được trống và số điện thoại phải đúng định dạng.");

            var userId = User.GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Back(nameof(Profile), error: "Không tìm thấy tài khoản.");

            user.FullName = input.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
            user.UpdatedAt = DateTime.Now;
            Log(userId, "update", "Cập nhật hồ sơ cá nhân");
            await _context.SaveChangesAsync();

            // Cookie đang giữ tên cũ: cấp lại để Topbar hiển thị tên mới ngay.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };
            var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
            var current = await HttpContext.AuthenticateAsync(scheme);
            await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)), current.Properties);

            return Back(nameof(Profile), ok: "Đã lưu thông tin cá nhân.");
        }

        private async Task<ProfileViewModel> BuildProfileAsync()
        {
            var userId = User.GetUserId();
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            var tokens = await _context.UserTokens.AsNoTracking()
                .Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).Take(10)
                .Select(t => new SecurityTokenItem { TokenType = t.TokenType, CreatedAt = t.CreatedAt, ExpiresAt = t.ExpiresAt, UsedAt = t.UsedAt })
                .ToListAsync();

            return new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailVerified = user.EmailVerified,
                RoleLabel = user.Role == UserRole.Admin ? "Quản trị viên" : "Người dùng",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                SecurityTokens = tokens
            };
        }

        // ---------------------------------------------------------------- 3. Giao diện
        [HttpGet]
        public async Task<IActionResult> Appearance()
        {
            Page("Giao diện");
            var s = await GetSettingsAsync(User.GetUserId());
            ViewBag.Timezones = Timezones;
            return View(new SettingsViewModel
            {
                ThemeMode = s.ThemeMode == "dark" ? "dark" : "light",
                Language = s.Language,
                Timezone = s.Timezone ?? "Asia/Ho_Chi_Minh"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppearance(UpdateAppearanceInput input)
        {
            if (!ModelState.IsValid || input.ThemeMode is not ("light" or "dark") || input.Language != "vi" || !Timezones.Contains(input.Timezone))
                return Back(nameof(Appearance), error: "Lựa chọn giao diện không hợp lệ.");

            var userId = User.GetUserId();
            var s = await GetSettingsAsync(userId);
            s.ThemeMode = input.ThemeMode;
            s.Language = input.Language;
            s.Timezone = input.Timezone;
            Log(userId, "update", "Thay đổi cài đặt giao diện");
            await _context.SaveChangesAsync();
            return Back(nameof(Appearance), ok: "Đã lưu cài đặt giao diện.");
        }

        // ---------------------------------------------------------------- 4. Thông báo
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            Page("Cài đặt thông báo");
            var userId = User.GetUserId();
            var s = await GetSettingsAsync(userId);
            ViewBag.Unread = await _context.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);
            ViewBag.PendingReminders = await _context.Reminders.AsNoTracking().CountAsync(r => r.UserId == userId && !r.IsSent);
            return View(new SettingsViewModel { NotifyEmail = s.NotifyEmail, NotifyPush = s.NotifyPush });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotifications(UpdateNotificationSettingsInput input)
        {
            var userId = User.GetUserId();
            var s = await GetSettingsAsync(userId);
            s.NotifyEmail = input.NotifyEmail;
            s.NotifyPush = input.NotifyPush;
            Log(userId, "update", "Thay đổi cài đặt thông báo");
            await _context.SaveChangesAsync();
            return Back(nameof(Notifications), ok: "Đã lưu cài đặt thông báo.");
        }

        // ---------------------------------------------------------------- 5. Bảo mật
        [HttpGet]
        public async Task<IActionResult> Security()
        {
            Page("Bảo mật");
            return View(await BuildProfileAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
        {
            if (!ModelState.IsValid) return Back(nameof(Security), error: "Mật khẩu mới phải có ít nhất 8 ký tự và nhập lại phải trùng khớp.");

            var userId = User.GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Back(nameof(Security), error: "Không tìm thấy tài khoản.");

            var hasher = new PasswordHasher<User>();
            var check = hasher.VerifyHashedPassword(user, user.PasswordHash, input.CurrentPassword);
            if (check == PasswordVerificationResult.Failed) return Back(nameof(Security), error: "Mật khẩu hiện tại không đúng.");

            user.PasswordHash = hasher.HashPassword(user, input.NewPassword);
            user.UpdatedAt = DateTime.Now;
            Log(userId, "security", "Đổi mật khẩu");
            await _context.SaveChangesAsync();
            return Back(nameof(Security), ok: "Đã đổi mật khẩu.");
        }

        // ---------------------------------------------------------------- 6. Chia sẻ & phân quyền
        [HttpGet]
        public async Task<IActionResult> Sharing()
        {
            Page("Chia sẻ & phân quyền");
            var userId = User.GetUserId();

            var shares = await _context.SharedItems.AsNoTracking()
                .Where(x => x.OwnerId == userId || x.SharedWithUserId == userId)
                .OrderByDescending(x => x.CreatedAt).ToListAsync();

            var userIds = shares.Select(x => x.OwnerId).Concat(shares.Select(x => x.SharedWithUserId)).Distinct().ToList();
            var people = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email }).ToDictionaryAsync(u => u.Id);

            var noteIds = shares.Where(x => x.ItemType == "note").Select(x => x.ItemId).ToList();
            var docIds = shares.Where(x => x.ItemType == "document").Select(x => x.ItemId).ToList();
            var noteTitles = await _context.Notes.AsNoTracking().Where(n => noteIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Title);
            var docTitles = await _context.Documents.AsNoTracking().Where(d => docIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.FileName);

            SharedItemRow Row(SharedItem x, int personId) => new()
            {
                Id = x.Id,
                ItemType = x.ItemType,
                ItemTitle = (x.ItemType == "document" ? docTitles.GetValueOrDefault(x.ItemId) : noteTitles.GetValueOrDefault(x.ItemId)) ?? "(đã xóa)",
                PersonName = people.TryGetValue(personId, out var p) ? p.FullName : "Người dùng",
                PersonEmail = people.TryGetValue(personId, out var p2) ? p2.Email : string.Empty,
                Permission = x.Permission,
                CreatedAt = x.CreatedAt
            };

            var vm = new SharingPageViewModel
            {
                SharedByMe = shares.Where(x => x.OwnerId == userId).Select(x => Row(x, x.SharedWithUserId)).ToList(),
                SharedWithMe = shares.Where(x => x.SharedWithUserId == userId).Select(x => Row(x, x.OwnerId)).ToList()
            };

            vm.Shareable = (await _context.Notes.AsNoTracking().Where(n => n.UserId == userId).OrderBy(n => n.Title)
                    .Select(n => new ShareableItem { Id = n.Id, ItemType = "note", Title = n.Title }).ToListAsync())
                .Concat(await _context.Documents.AsNoTracking().Where(d => d.UserId == userId).OrderBy(d => d.FileName)
                    .Select(d => new ShareableItem { Id = d.Id, ItemType = "document", Title = d.FileName }).ToListAsync())
                .ToList();

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(ShareItemInput input)
        {
            if (!ModelState.IsValid || input.Permission is not ("view" or "edit"))
                return Back(nameof(Sharing), error: "Thông tin chia sẻ chưa hợp lệ.");

            var parts = input.ItemKey.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var itemId) || parts[0] is not ("note" or "document"))
                return Back(nameof(Sharing), error: "Nội dung chia sẻ không hợp lệ.");

            var userId = User.GetUserId();
            var owns = parts[0] == "note"
                ? await _context.Notes.AnyAsync(n => n.Id == itemId && n.UserId == userId)
                : await _context.Documents.AnyAsync(d => d.Id == itemId && d.UserId == userId);
            if (!owns) return Back(nameof(Sharing), error: "Bạn chỉ có thể chia sẻ nội dung của chính mình.");

            var email = input.Email.Trim().ToLower();
            var target = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Active);
            if (target == null) return Back(nameof(Sharing), error: "Không tìm thấy người dùng với email này.");
            if (target.Id == userId) return Back(nameof(Sharing), error: "Bạn không thể chia sẻ cho chính mình.");

            var existing = await _context.SharedItems.FirstOrDefaultAsync(x =>
                x.OwnerId == userId && x.SharedWithUserId == target.Id && x.ItemType == parts[0] && x.ItemId == itemId);
            if (existing != null) existing.Permission = input.Permission;
            else _context.SharedItems.Add(new SharedItem
            {
                OwnerId = userId, SharedWithUserId = target.Id, ItemType = parts[0], ItemId = itemId,
                Permission = input.Permission, CreatedAt = DateTime.Now
            });

            Log(userId, "share", $"Chia sẻ {parts[0]} #{itemId} cho {target.Email}");
            await _context.SaveChangesAsync();
            return Back(nameof(Sharing), ok: "Đã chia sẻ cho " + target.FullName + ".");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSharePermission(UpdateSharePermissionInput input)
        {
            if (!ModelState.IsValid || input.Permission is not ("view" or "edit"))
                return Back(nameof(Sharing), error: "Quyền không hợp lệ.");

            var userId = User.GetUserId();
            var share = await _context.SharedItems.FirstOrDefaultAsync(x => x.Id == input.Id && x.OwnerId == userId);
            if (share == null) return Back(nameof(Sharing), error: "Không tìm thấy lượt chia sẻ của bạn.");

            share.Permission = input.Permission;
            Log(userId, "share", $"Đổi quyền chia sẻ #{share.Id} thành {input.Permission}");
            await _context.SaveChangesAsync();
            return Back(nameof(Sharing), ok: "Đã cập nhật quyền.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeShare(int id)
        {
            var userId = User.GetUserId();
            // Chủ sở hữu thu hồi quyền; người được chia sẻ có thể tự rời khỏi mục.
            var share = await _context.SharedItems.FirstOrDefaultAsync(x => x.Id == id && (x.OwnerId == userId || x.SharedWithUserId == userId));
            if (share == null) return Back(nameof(Sharing), error: "Không tìm thấy lượt chia sẻ.");

            _context.SharedItems.Remove(share);
            Log(userId, "share", $"Thu hồi chia sẻ #{id}");
            await _context.SaveChangesAsync();
            return Back(nameof(Sharing), ok: "Đã thu hồi quyền truy cập.");
        }

        // ---------------------------------------------------------------- 7. Nhật ký hoạt động
        [HttpGet]
        public async Task<IActionResult> ActivityLog()
        {
            Page("Nhật ký hoạt động");
            var userId = User.GetUserId();
            var name = User.Identity?.Name ?? "Bạn";
            var logs = await _context.SystemLogs.AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt).Take(200)
                .Select(l => new SystemLogItem
                {
                    Id = l.Id, UserName = name, Action = l.Action, Details = l.Details ?? string.Empty,
                    IpAddress = l.IpAddress ?? string.Empty, CreatedAt = l.CreatedAt
                })
                .ToListAsync();
            return View(new SystemLogPageViewModel { IsAdminView = false, Logs = logs });
        }

        // ---------------------------------------------------------------- 8. Thành tựu của tôi (mục trong menu Avatar)
        [HttpGet]
        public async Task<IActionResult> Achievements()
        {
            Page("Thành tựu của tôi");
            var userId = User.GetUserId();
            var earned = await _context.UserAchievements.AsNoTracking()
                .Where(u => u.UserId == userId).ToDictionaryAsync(u => u.AchievementId, u => u.EarnedAt);
            var items = (await _context.Achievements.AsNoTracking().OrderBy(a => a.PointsReward).ToListAsync())
                .Select(a => new AchievementItem
                {
                    Id = a.Id, BadgeCode = a.BadgeCode, Title = a.Title, Description = a.Description ?? string.Empty,
                    Icon = string.IsNullOrWhiteSpace(a.BadgeIcon) ? "★" : a.BadgeIcon!, PointsReward = a.PointsReward,
                    EarnedAt = earned.TryGetValue(a.Id, out var at) ? at : null
                }).ToList();
            return View(new AchievementPageViewModel { Items = items });
        }

        // ---------------------------------------------------------------- 9. Trợ giúp & hỗ trợ
        [HttpGet]
        public IActionResult Help()
        {
            Page("Trợ giúp & hỗ trợ");
            return View();
        }
    }
}
