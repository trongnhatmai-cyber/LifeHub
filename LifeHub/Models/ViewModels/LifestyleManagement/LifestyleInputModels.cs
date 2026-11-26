using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.LifestyleManagement
{
    // Dữ liệu form, chuẩn bị sẵn cho khi có action POST. Tên thuộc tính trùng name="" trong _LifeModals.cshtml.
    // Hiện tại các form chạy mô phỏng (data-simulate), chưa lưu dữ liệu.

    /// <summary>Ghi nhận giấc ngủ -> lifestyle_logs (SleepHours, BedTime, WakeTime, SleepQuality, Notes). Mỗi ngày một dòng.</summary>
    public class SaveSleepInput
    {
        public int Id { get; set; }
        [Required] public DateOnly SleepDate { get; set; }
        [Required] public TimeOnly BedTime { get; set; }
        [Required] public TimeOnly WakeTime { get; set; }
        [Range(1, 5)] public int? SleepQuality { get; set; }
        [StringLength(255)] public string? Notes { get; set; }
    }

    /// <summary>
    /// Ghi nhận hoạt động. Cột có sẵn trong lifestyle_logs: PhysicalActivityMinutes, ActivityType, Notes.
    /// StartTime, Location, PlanActivityId chưa có cột lưu: cần Backend bổ sung bảng/cột trước khi POST thật.
    /// </summary>
    public class SaveActivityInput
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required] public string Category { get; set; } = "other";
        [Required] public DateOnly ActivityDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        [Range(1, 1440)] public int DurationMin { get; set; } = 30;
        [StringLength(150)] public string? Location { get; set; }
        [StringLength(255)] public string? Notes { get; set; }
        public int? PlanActivityId { get; set; }
    }

    public class SavePlanActivityInput
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required] public TimeOnly Time { get; set; }
        [Range(0, 600)] public int DurationMin { get; set; }
        public List<int> Days { get; set; } = new();        // DayOfWeek: 0 = Chủ nhật ... 6 = Thứ bảy
        public int ReminderMin { get; set; }
    }

    /// <summary>Tạo hoặc sửa kế hoạch (chưa có bảng lưu: cần Backend).</summary>
    public class SavePlanInput
    {
        public int Id { get; set; }
        [Required, StringLength(120)] public string Name { get; set; } = "";
        [StringLength(255)] public string? Description { get; set; }
        [Required] public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [Required] public string Frequency { get; set; } = "daily";   // daily / weekly / custom
        public List<SavePlanActivityInput> Activities { get; set; } = new();
    }

    /// <summary>Áp dụng kế hoạch vào lịch cá nhân: tạo mục lịch (events) nguồn Lifestyle, không tạo trùng khi áp dụng lại.</summary>
    public class ApplyPlanInput
    {
        [Required] public int PlanId { get; set; }
        [Required] public DateOnly FromDate { get; set; }
        [Required] public DateOnly ToDate { get; set; }
    }
}
