using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.ViewModels.LifestyleManagement
{
    // ViewModel cho 4 trang Sinh hoạt cá nhân: Tổng quan, Giấc ngủ, Hoạt động thể chất, Kế hoạch.
    // Mọi con số do Controller tính sẵn, View chỉ in ra. Không sửa model entity.
    //
    // NGUỒN DỮ LIỆU
    //   - Giấc ngủ, vận động theo ngày: bảng lifestyle_logs (LifestyleLog), dữ liệu thật.
    //   - Kế hoạch sinh hoạt: CHƯA có bảng trong CSDL, nên dùng dữ liệu mẫu (LifestylePlanSample).
    //     Khi có bảng, chỉ cần thay nguồn nạp ở Controller; View và ViewModel giữ nguyên.

    // ======================= TIỆN ÍCH ĐỊNH DẠNG =======================
    public static class LifeFmt
    {
        /// <summary>450 phút -> "7h 30p"; 480 -> "8h"; 45 -> "45p".</summary>
        public static string Duration(int? minutes)
        {
            if (!minutes.HasValue) return "—";
            var m = Math.Max(0, minutes.Value);
            var h = m / 60; var r = m % 60;
            return h == 0 ? $"{r}p" : r == 0 ? $"{h}h" : $"{h}h {r:00}p";
        }

        public static string Clock(TimeOnly? t) => t.HasValue ? t.Value.ToString("HH:mm") : "—";
        public static string Clock(int minutesOfDay) => $"{(minutesOfDay % 1440) / 60:00}:{(minutesOfDay % 1440) % 60:00}";

        public static string Quality(int? q) => q.HasValue ? $"{q.Value}/5" : "—";

        /// <summary>Thứ trong tuần theo tiếng Việt (DayOfWeek -> T2..CN).</summary>
        public static string DayShort(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => "T2", DayOfWeek.Tuesday => "T3", DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5", DayOfWeek.Friday => "T6", DayOfWeek.Saturday => "T7", _ => "CN"
        };
    }

    /// <summary>Danh mục hoạt động thể chất (mục 10.3). Khóa ASCII, nhãn tiếng Việt.</summary>
    public static class LifeCategory
    {
        public static readonly (string Key, string Label, string Example)[] All =
        {
            ("walk", "Đi bộ", "Đi bộ ngoài trời"),
            ("run", "Chạy bộ", "Chạy bộ nhẹ"),
            ("sport", "Thể thao", "Cầu lông, bóng đá"),
            ("workout", "Tập luyện", "Yoga, thể dục"),
            ("light", "Vận động nhẹ", "Giãn cơ, đi dạo"),
            ("other", "Khác", "Tự nhập")
        };

        public static string Label(string key) => All.FirstOrDefault(c => c.Key == key).Label ?? "Khác";

        /// <summary>lifestyle_logs.ActivityType là văn bản tự do, nên suy ra danh mục từ từ khóa. Không khớp thì là "other".</summary>
        public static string FromText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "other";
            var t = text.Trim().ToLowerInvariant();
            if (All.Any(c => c.Key == t)) return t;
            bool Has(params string[] keys) => keys.Any(k => t.Contains(k));
            if (Has("đi bộ", "walk")) return "walk";
            if (Has("chạy", "run", "jog")) return "run";
            if (Has("cầu lông", "bóng", "bơi", "tennis", "sport", "thể thao", "đạp xe", "cycl")) return "sport";
            if (Has("yoga", "gym", "tập", "thể dục", "workout", "fitness")) return "workout";
            if (Has("giãn", "stretch", "đi dạo")) return "light";
            return "other";
        }
    }

    // ======================= GIẤC NGỦ =======================
    public class LifeSleepItem
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? Bed { get; set; }
        public TimeOnly? Wake { get; set; }
        public int? Minutes { get; set; }
        public int? Quality { get; set; }
        public string Notes { get; set; } = "";

        public string DateLabel => Date.ToString("dd/MM");
        public string DateKey => Date.ToString("yyyy-MM-dd");
        public string BedLabel => LifeFmt.Clock(Bed);
        public string WakeLabel => LifeFmt.Clock(Wake);
        public string DurationLabel => LifeFmt.Duration(Minutes);
        public string QualityLabel => LifeFmt.Quality(Quality);
    }

    /// <summary>Một cột trong biểu đồ thời lượng ngủ (thang cố định 0 đến 10 giờ).</summary>
    public class LifeSleepBar
    {
        public string Label { get; set; } = "";
        public int? Minutes { get; set; }
        public int Percent { get; set; }
        public bool IsToday { get; set; }
        public string ValueLabel => LifeFmt.Duration(Minutes);
    }

    /// <summary>Một dòng trong biểu đồ giờ đi ngủ và thức dậy: thanh trôi từ 20:00 đến 12:00 hôm sau.</summary>
    public class LifeSleepSpan
    {
        public string Label { get; set; } = "";
        public bool HasData { get; set; }
        public double LeftPercent { get; set; }
        public double WidthPercent { get; set; }
        public string BedLabel { get; set; } = "—";
        public string WakeLabel { get; set; } = "—";
    }

    public class LifeSleepStats
    {
        public int? LastMinutes { get; set; }
        public string LastNote { get; set; } = "Chưa có ghi nhận";
        public int? AverageMinutes { get; set; }
        public int RecordedDays { get; set; }               // số ngày có dữ liệu trong 7 ngày
        public string AverageBed { get; set; } = "—";
        public string AverageWake { get; set; } = "—";
        public List<LifeSleepBar> Bars { get; set; } = new();
        public List<LifeSleepSpan> Spans { get; set; } = new();
    }

    public class LifeSleepPageViewModel
    {
        public string Range { get; set; } = "30";           // 7 / 30 / 90
        public LifeSleepStats Stats { get; set; } = new();
        public int TotalInRange { get; set; }
        public List<LifeSleepItem> Items { get; set; } = new();
    }

    // ======================= HOẠT ĐỘNG THỂ CHẤT =======================
    public class LifeActivityItem
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Name { get; set; } = "";
        public string CategoryKey { get; set; } = "other";
        public int Minutes { get; set; }
        public string Notes { get; set; } = "";

        public string CategoryLabel => LifeCategory.Label(CategoryKey);
        public string DateLabel => Date.ToString("dd/MM/yyyy");
        public string DateKey => Date.ToString("yyyy-MM-dd");
        public string DurationLabel => LifeFmt.Duration(Minutes);
    }

    public class LifeDayBar
    {
        public string Label { get; set; } = "";
        public int Minutes { get; set; }
        public int Percent { get; set; }
        public bool IsToday { get; set; }
        public string ValueLabel => Minutes == 0 ? "—" : LifeFmt.Duration(Minutes);
    }

    public class LifeBand
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public int Minutes { get; set; }
        public int Sessions { get; set; }
        public int Percent { get; set; }
    }

    public class LifeWeekCompare
    {
        public int ThisWeekMinutes { get; set; }
        public int LastWeekMinutes { get; set; }
        public int ThisPercent { get; set; }
        public int LastPercent { get; set; }
        public string DeltaLabel { get; set; } = "";
        public string DeltaTone { get; set; } = "flat";     // up / down / flat
    }

    public class LifeActivityStats
    {
        public int TodayMinutes { get; set; }
        public int WeekMinutes { get; set; }
        public int WeekSessions { get; set; }
        public string LatestName { get; set; } = "Chưa có";
        public string LatestNote { get; set; } = "Chưa ghi nhận hoạt động nào";
        public List<LifeDayBar> Bars { get; set; } = new();
        public List<LifeBand> Bands { get; set; } = new();
        public LifeWeekCompare Compare { get; set; } = new();
    }

    /// <summary>Lựa chọn trong ô "Liên kết hoạt động trong kế hoạch" của modal ghi nhận hoạt động.</summary>
    public class LifePlanOption
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
    }

    public class LifeActivityPageViewModel
    {
        public string Range { get; set; } = "30";           // 7 / 30 / 90
        public string Category { get; set; } = "all";
        public LifeActivityStats Stats { get; set; } = new();
        public int TotalInRange { get; set; }
        public List<LifeActivityItem> Items { get; set; } = new();
        public List<LifePlanOption> PlanOptions { get; set; } = new();
    }

    // ======================= KẾ HOẠCH SINH HOẠT =======================
    public class LifePlanActivity
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public string Name { get; set; } = "";
        public TimeOnly Time { get; set; }
        public int DurationMin { get; set; }
        public List<DayOfWeek> Days { get; set; } = new();
        public int ReminderMin { get; set; }                // 0 = không nhắc
        public bool MockDone { get; set; }                  // dữ liệu mẫu: đã thực hiện hay chưa
        public int WeekDone { get; set; }                   // dữ liệu mẫu: số lần đã làm trong tuần
        public int WeekExpected { get; set; }               // số lần cần làm trong tuần
        /// <summary>done / missed / pending, tính theo ngày đang xem và giờ hiện tại.</summary>
        public string State { get; set; } = "pending";

        public string TimeLabel => Time.ToString("HH:mm");
        public string DurationLabel => DurationMin <= 0 ? "—" : LifeFmt.Duration(DurationMin);
        public string DaysLabel => Days.Count == 7 ? "Hằng ngày" : string.Join(", ", Days.OrderBy(d => ((int)d + 6) % 7).Select(LifeFmt.DayShort));
        public string ReminderLabel => ReminderMin <= 0 ? "Không nhắc" : ReminderMin >= 60 ? $"Trước {ReminderMin / 60} giờ" : $"Trước {ReminderMin} phút";
        public string StateLabel => State switch { "done" => "Hoàn thành", "missed" => "Chưa thực hiện", _ => "Sắp tới" };
        public string StateClass => State switch { "done" => "st-done", "missed" => "st-skipped", _ => "st-pending" };
        public string WeekLabel => WeekExpected == 0 ? "—" : $"{WeekDone}/{WeekExpected}";
    }

    public class LifePlanHistoryDay
    {
        public string Label { get; set; } = "";
        public int Done { get; set; }
        public int Total { get; set; }
        public int Percent => Total == 0 ? 0 : (int)Math.Round(Done * 100d / Total);
        public bool IsToday { get; set; }
    }

    public class LifePlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string FrequencyLabel { get; set; } = "Hằng ngày";
        public bool IsActive { get; set; } = true;
        public List<LifePlanActivity> Activities { get; set; } = new();
        public List<LifePlanHistoryDay> History { get; set; } = new();

        public int WeekDone => Activities.Sum(a => a.WeekDone);
        public int WeekExpected => Activities.Sum(a => a.WeekExpected);
        public int WeekPercent => WeekExpected == 0 ? 0 : (int)Math.Round(WeekDone * 100d / WeekExpected);
        public string StatusLabel => IsActive ? "Đang áp dụng" : "Tạm dừng";
        public string StatusClass => IsActive ? "st-doing" : "st-paused";
        public string PeriodLabel => EndDate.HasValue ? $"{StartDate:dd/MM/yyyy} – {EndDate:dd/MM/yyyy}" : $"Từ {StartDate:dd/MM/yyyy}, không giới hạn";
        public int TotalMinutes => Activities.Sum(a => a.DurationMin);
    }

    public class LifePlanPageViewModel
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int PausedCount { get; set; }
        public int ActivityCount { get; set; }
        public List<LifePlan> Plans { get; set; } = new();
        public LifePlan? Selected { get; set; }
    }

    // ======================= TỔNG QUAN =======================
    public class LifeOverviewViewModel
    {
        public DateOnly Date { get; set; }
        public bool IsToday { get; set; }
        public string DateKey => Date.ToString("yyyy-MM-dd");
        public string DateLabel => Date.ToString("dd/MM/yyyy");
        public string PrevKey { get; set; } = "";
        public string? NextKey { get; set; }

        // 4 thẻ chỉ số
        public int? LastSleepMinutes { get; set; }
        public string LastSleepNote { get; set; } = "Chưa có ghi nhận";
        public int ActivityMinutes { get; set; }
        public string ActivityNote { get; set; } = "Chưa ghi nhận";
        public int PlanDone { get; set; }
        public int PlanScheduled { get; set; }

        /// <summary>Các kế hoạch đang áp dụng có hoạt động trong ngày, mỗi kế hoạch chỉ chứa hoạt động của ngày đó.</summary>
        public List<LifePlan> TodayPlans { get; set; } = new();

        public LifeSleepStats Sleep { get; set; } = new();
        public LifeActivityStats Activity { get; set; } = new();

        /// <summary>Bản ghi vận động thực tế (khác với hoạt động được lên kế hoạch).</summary>
        public List<LifeActivityItem> Recent { get; set; } = new();
        public List<LifePlanOption> PlanOptions { get; set; } = new();
    }

    // ======================= DỮ LIỆU MẪU CHO KẾ HOẠCH =======================
    /// <summary>
    /// Nguồn dữ liệu mẫu cho "Kế hoạch sinh hoạt" cho tới khi có bảng lưu trong CSDL.
    /// Controller chỉ gọi Build(); nếu sau này đọc từ DB, thay đúng một dòng gọi này.
    /// </summary>
    public static class LifestylePlanSample
    {
        private static readonly DayOfWeek[] Every =
            { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        private static LifePlanActivity Act(int id, int plan, string planName, string name, string time, int minutes,
            DayOfWeek[] days, int remind, bool done, int weekDone, int weekExpected) => new()
        {
            Id = id, PlanId = plan, PlanName = planName, Name = name, Time = TimeOnly.Parse(time), DurationMin = minutes,
            Days = days.ToList(), ReminderMin = remind, MockDone = done, WeekDone = weekDone, WeekExpected = weekExpected
        };

        /// <summary>Dựng danh sách kế hoạch mẫu, gắn trạng thái từng hoạt động cho ngày <paramref name="day"/>.</summary>
        public static List<LifePlan> Build(DateOnly day, DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            var plans = new List<LifePlan>
            {
                new()
                {
                    Id = 1, Name = "Sinh hoạt buổi sáng", Description = "Bắt đầu ngày mới đều đặn và nhẹ nhàng.",
                    StartDate = today.AddDays(-45), FrequencyLabel = "Hằng ngày", IsActive = true,
                    Activities =
                    {
                        Act(11, 1, "Sinh hoạt buổi sáng", "Thức dậy", "06:30", 5, Every, 0, true, 6, 7),
                        Act(12, 1, "Sinh hoạt buổi sáng", "Vệ sinh cá nhân", "06:40", 15, Every, 0, true, 6, 7),
                        Act(13, 1, "Sinh hoạt buổi sáng", "Ăn sáng", "07:00", 30, Every, 10, true, 5, 7),
                        Act(14, 1, "Sinh hoạt buổi sáng", "Vận động nhẹ", "07:30", 20, Every, 10, false, 4, 7)
                    }
                },
                new()
                {
                    Id = 2, Name = "Sinh hoạt buổi tối", Description = "Thư giãn và đi ngủ đúng giờ.",
                    StartDate = today.AddDays(-45), FrequencyLabel = "Hằng ngày", IsActive = true,
                    Activities =
                    {
                        Act(21, 2, "Sinh hoạt buổi tối", "Ăn tối", "19:00", 45, Every, 15, true, 7, 7),
                        Act(22, 2, "Sinh hoạt buổi tối", "Đọc sách", "21:00", 30, Every, 0, true, 7, 7),
                        Act(23, 2, "Sinh hoạt buổi tối", "Đi ngủ", "22:30", 0, Every, 30, true, 7, 7)
                    }
                },
                new()
                {
                    Id = 3, Name = "Vận động trong tuần", Description = "Ba buổi vận động cố định mỗi tuần.",
                    StartDate = today.AddDays(-30), FrequencyLabel = "3 buổi/tuần", IsActive = true,
                    Activities =
                    {
                        Act(31, 3, "Vận động trong tuần", "Chạy bộ nhẹ", "06:00", 30, new[] { DayOfWeek.Monday }, 15, true, 1, 1),
                        Act(32, 3, "Vận động trong tuần", "Tập yoga", "18:00", 45, new[] { DayOfWeek.Wednesday }, 30, true, 1, 1),
                        Act(33, 3, "Vận động trong tuần", "Đạp xe", "17:30", 60, new[] { DayOfWeek.Friday }, 30, false, 0, 1)
                    }
                },
                new()
                {
                    Id = 4, Name = "Cuối tuần thư thái", Description = "Đọc sách và viết nhật ký vào cuối tuần.",
                    StartDate = today.AddDays(-60), EndDate = today.AddDays(30), FrequencyLabel = "Hằng tuần", IsActive = false,
                    Activities =
                    {
                        Act(41, 4, "Cuối tuần thư thái", "Đọc sách", "20:00", 60, new[] { DayOfWeek.Saturday }, 0, false, 0, 0),
                        Act(42, 4, "Cuối tuần thư thái", "Viết nhật ký", "21:00", 20, new[] { DayOfWeek.Sunday }, 0, false, 0, 0)
                    }
                }
            };

            foreach (var p in plans)
            {
                foreach (var a in p.Activities) a.State = StateOf(a, day, now);

                // Lịch sử 7 ngày gần nhất (dữ liệu mẫu, cố định theo ngày để không nhấp nháy giữa các lần tải).
                for (int i = 6; i >= 0; i--)
                {
                    var d = today.AddDays(-i);
                    var total = p.Activities.Count(a => a.Days.Contains(d.DayOfWeek));
                    var done = i == 0
                        ? p.Activities.Where(a => a.Days.Contains(d.DayOfWeek)).Count(a => StateOf(a, d, now) == "done")
                        : Math.Max(0, total - ((d.DayNumber + p.Id) % 3 == 0 && total > 0 ? 1 : 0));
                    p.History.Add(new LifePlanHistoryDay { Label = d.ToString("dd/MM"), Done = done, Total = total, IsToday = i == 0 });
                }
            }
            return plans;
        }

        private static string StateOf(LifePlanActivity a, DateOnly day, DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            if (day > today) return "pending";
            if (day == today && a.Time.ToTimeSpan() > now.TimeOfDay) return "pending";
            return a.MockDone ? "done" : "missed";
        }
    }
}
