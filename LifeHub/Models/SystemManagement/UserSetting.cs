using System;


namespace LifeHub.Models.SystemManagement;

public class UserSettings
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ThemeMode { get; set; } = "light";

    public string Language { get; set; } = "vi";

    public string? Timezone { get; set; } = "Asia/Ho_Chi_Minh";

    public string? DashboardLayout { get; set; }

    public string? VisibleModules { get; set; }

    public int PomodoroWorkMin { get; set; } = 25;

    public int PomodoroBreakMin { get; set; } = 5;

    public int PomodoroLongBreakMin { get; set; } = 15;

    public bool NotifyEmail { get; set; } = true;

    public bool NotifyPush { get; set; } = true;
}
