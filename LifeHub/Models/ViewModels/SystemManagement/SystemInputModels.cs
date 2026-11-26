using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.SystemManagement
{
    // Tên thuộc tính trùng với name="" của input trong các View tương ứng.

    /// <summary>Form "Thông tin cá nhân" (bảng users).</summary>
    public class UpdateProfileInput
    {
        [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
        [Phone, StringLength(15)] public string? PhoneNumber { get; set; }
    }

    /// <summary>Form "Đổi mật khẩu" (bảng users.PasswordHash).</summary>
    public class ChangePasswordInput
    {
        [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(8), DataType(DataType.Password)] public string NewPassword { get; set; } = string.Empty;
        [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>Form "Cài đặt" (bảng user_settings).</summary>
    public class UpdateSettingsInput
    {
        [Required] public string ThemeMode { get; set; } = "light";
        [Required] public string Language { get; set; } = "vi";
        public string? Timezone { get; set; }
        public bool NotifyEmail { get; set; }
        public bool NotifyPush { get; set; }
        [Range(1, 180)] public int PomodoroWorkMin { get; set; } = 25;
        [Range(1, 60)] public int PomodoroBreakMin { get; set; } = 5;
        [Range(1, 120)] public int PomodoroLongBreakMin { get; set; } = 15;
        public List<string> VisibleModules { get; set; } = new();
        public List<string> DashboardWidgets { get; set; } = new();
    }

    /// <summary>Form "Chia sẻ mới" (bảng shared_items).</summary>
    public class ShareItemInput
    {
        [Required] public string ItemKey { get; set; } = string.Empty;     // dạng "note:5" hoặc "document:3"
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Permission { get; set; } = "view";        // view / edit
    }

    /// <summary>Đổi quyền của một lượt chia sẻ.</summary>
    public class UpdateSharePermissionInput
    {
        [Required] public int Id { get; set; }
        [Required] public string Permission { get; set; } = "view";
    }

    /// <summary>Form "Giao diện" (user_settings: ThemeMode, Language, Timezone).</summary>
    public class UpdateAppearanceInput
    {
        [Required] public string ThemeMode { get; set; } = "light";
        [Required] public string Language { get; set; } = "vi";
        [Required] public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    }

    /// <summary>Form "Thông báo" (user_settings: NotifyEmail, NotifyPush).</summary>
    public class UpdateNotificationSettingsInput
    {
        public bool NotifyEmail { get; set; }
        public bool NotifyPush { get; set; }
    }
}
