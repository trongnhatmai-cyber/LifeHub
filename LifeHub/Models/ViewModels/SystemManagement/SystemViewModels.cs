using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.SystemManagement
{
    public class ReminderItem
    {
        public int Id { get; set; }
        public string RefType { get; set; } = "task";                  // task / assignment / event / habit
        public string Title { get; set; } = string.Empty;
        public DateTime RemindAt { get; set; }
        public int? OffsetMinutes { get; set; }
        public bool IsSent { get; set; }

        public string RefTypeLabel => RefType switch
        {
            "assignment" => "Bài tập", "event" => "Sự kiện", "habit" => "Thói quen", _ => "Công việc"
        };

        public string OffsetLabel => !OffsetMinutes.HasValue ? "Đúng giờ"
            : OffsetMinutes >= 1440 ? $"Trước {OffsetMinutes / 1440} ngày"
            : OffsetMinutes >= 60 ? $"Trước {OffsetMinutes / 60} giờ"
            : $"Trước {OffsetMinutes} phút";
    }

    // ======================= TÌM KIẾM TOÀN HỆ THỐNG =======================
    public class SearchPageViewModel
    {
        public string Query { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;               // rỗng = tất cả loại
        public int TotalCount { get; set; }
        public List<SearchTypeOption> Types { get; set; } = new();
        public List<SearchGroup> Groups { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public class SearchTypeOption
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SearchGroup
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<SearchResultItem> Items { get; set; } = new();
    }

    public class SearchResultItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;               // task / project / course / note / document / event
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string Controller { get; set; } = "Dashboard";          // nơi mở kết quả
        public string Action { get; set; } = "Index";
    }

    // ======================= CHIA SẺ & PHÂN QUYỀN (shared_items) =======================
    public class SharingPageViewModel
    {
        public List<SharedItemRow> SharedByMe { get; set; } = new();
        public List<SharedItemRow> SharedWithMe { get; set; } = new();
        public List<ShareableItem> Shareable { get; set; } = new();    // ghi chú, tài liệu của tôi có thể chia sẻ
    }

    public class SharedItemRow
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = "note";                 // note / document
        public string ItemTitle { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;         // người nhận (hoặc chủ sở hữu nếu là mục "được chia sẻ với tôi")
        public string PersonEmail { get; set; } = string.Empty;
        public string Permission { get; set; } = "view";               // view / edit
        public DateTime CreatedAt { get; set; }

        public string ItemTypeLabel => ItemType == "document" ? "Tài liệu" : "Ghi chú";
        public string PermissionLabel => Permission == "edit" ? "Được sửa" : "Chỉ xem";
    }

    public class ShareableItem
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = "note";
        public string Title { get; set; } = string.Empty;
        public string Key => $"{ItemType}:{Id}";
    }

    // ======================= HỒ SƠ (users, user_tokens) =======================
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool EmailVerified { get; set; }
        public string RoleLabel { get; set; } = "Người dùng";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string Initial => string.IsNullOrWhiteSpace(FullName) ? "?" : FullName.Trim()[0].ToString().ToUpperInvariant();

        public int CompletedTasks { get; set; }
        public int ActiveProjects { get; set; }
        public int BestStreakDays { get; set; }
        public int TotalPoints { get; set; }

        public List<SecurityTokenItem> SecurityTokens { get; set; } = new();
    }

    public class SecurityTokenItem
    {
        public string TokenType { get; set; } = "verify_email";        // verify_email / reset_password
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public string TypeLabel => TokenType == "reset_password" ? "Đặt lại mật khẩu" : "Xác thực email";
        public string StatusKey(DateTime now) => UsedAt.HasValue ? "used" : ExpiresAt < now ? "expired" : "active";
        public string StatusLabel(DateTime now) => StatusKey(now) switch { "used" => "Đã sử dụng", "expired" => "Hết hạn", _ => "Còn hiệu lực" };
    }

    // ======================= CÀI ĐẶT (user_settings) =======================
    public class SettingsViewModel
    {
        public string ThemeMode { get; set; } = "light";               // light / dark / system
        public string Language { get; set; } = "vi";
        public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
        public bool NotifyEmail { get; set; } = true;
        public bool NotifyPush { get; set; } = true;
        public int PomodoroWorkMin { get; set; } = 25;
        public int PomodoroBreakMin { get; set; } = 5;
        public int PomodoroLongBreakMin { get; set; } = 15;
        public List<SettingToggle> Modules { get; set; } = new();           // user_settings.VisibleModules
        public List<SettingToggle> DashboardWidgets { get; set; } = new();  // user_settings.DashboardLayout
    }

    public class SettingToggle
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    // ======================= THÀNH TỰU & HUY HIỆU (achievements, user_achievements) =======================
    public class AchievementPageViewModel
    {
        public int TotalPoints => Items.Where(a => a.IsEarned).Sum(a => a.PointsReward);
        public int EarnedCount => Items.Count(a => a.IsEarned);
        public int TotalCount => Items.Count;
        public AchievementItem? Latest => Items.Where(a => a.IsEarned).OrderByDescending(a => a.EarnedAt).FirstOrDefault();
        public List<AchievementItem> Items { get; set; } = new();
    }

    public class AchievementItem
    {
        public int Id { get; set; }
        public string BadgeCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "★";
        public string Accent { get; set; } = "goal";                   // tên màu pastel trong layout.css: study, task, project, time, finance, goal, habit...
        public int PointsReward { get; set; }
        public DateTime? EarnedAt { get; set; }
        public int ProgressPercent { get; set; }                       // dùng khi chưa đạt
        public string ProgressLabel { get; set; } = string.Empty;      // "34 / 50 công việc"

        public bool IsEarned => EarnedAt.HasValue;
    }

    // ======================= NHẬT KÝ HỆ THỐNG (system_logs) =======================
    public class SystemLogPageViewModel
    {
        public bool IsAdminView { get; set; }
        public List<SystemLogItem> Logs { get; set; } = new();
    }

    public class SystemLogItem
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;            // login / logout / create / update / delete / share / security / export
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string ActionLabel => Action switch
        {
            "login" => "Đăng nhập", "logout" => "Đăng xuất", "create" => "Tạo mới", "update" => "Cập nhật",
            "delete" => "Xóa", "share" => "Chia sẻ", "security" => "Bảo mật", "export" => "Xuất dữ liệu", _ => Action
        };

        /// <summary>Nhóm dùng cho bộ lọc: session / data / share / security</summary>
        public string Group => Action switch
        {
            "login" or "logout" => "session",
            "create" or "update" or "delete" or "export" => "data",
            "share" => "share",
            _ => "security"
        };
    }
}
