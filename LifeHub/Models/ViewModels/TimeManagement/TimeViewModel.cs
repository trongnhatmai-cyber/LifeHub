namespace LifeHub.Models.ViewModels.TimeManagement
{
    // =====================================================================
    // VIEWMODEL PHÂN HỆ QUẢN LÝ THỜI GIAN
    // View chỉ in giá trị đã tính sẵn. Controller dựng ViewModel từ bảng events / pomodoro_sessions (chỉ đọc).
    // Khi Back-end đổi Event.EndTime sang DateTime và bổ sung các cột còn thiếu, chỉ cần sửa phần map
    // trong TimeController (hàm Map / ResolveEnd), View và ViewModel giữ nguyên.
    // =====================================================================

    /// <summary>Một sự kiện trên lịch tổng hợp (từ bất kỳ phân hệ nguồn nào).</summary>
    public class TimeEventItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Source { get; set; } = "personal";      // study / task / project / life / personal
        public string SourceLabel { get; set; } = "";
        public string TypeLabel { get; set; } = "";           // loại sự kiện (Họp, Học tập, Cá nhân...)
        public string Color { get; set; } = "#7c3aed";        // màu của phân hệ nguồn
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public string Reminder { get; set; } = "";
        public string Recurrence { get; set; } = "Không lặp";
        public string RecurrenceKey { get; set; } = "none";
        public string OriginLabel { get; set; } = "";         // "Quản lý thời gian", "Học tập"...
        public string OriginUrl { get; set; } = "";           // trang chỉnh sửa ở phân hệ nguồn (khi không sửa được tại đây)
        public bool Editable { get; set; }                    // chỉ sự kiện do Quản lý thời gian tạo mới sửa được tại đây
        public string Status { get; set; } = "upcoming";      // upcoming / ongoing / done / cancelled

        public int DurationMinutes => IsAllDay ? 0 : Math.Max(0, (int)(End - Start).TotalMinutes);
        public string TimeRange => IsAllDay ? "Cả ngày" : $"{Start:HH:mm} – {End:HH:mm}";
        public string DateLabel => Start.ToString("dd/MM/yyyy");
        public string StatusLabel => Status switch { "done" => "Đã hoàn thành", "ongoing" => "Đang diễn ra", "cancelled" => "Đã hủy", _ => "Sắp tới" };
        // Dùng lại 4 lớp trạng thái chuẩn của file mẫu: graded / submitted / upcoming / overdue
        public string StatusClass => Status switch { "done" => "st-graded", "ongoing" => "st-submitted", "cancelled" => "st-overdue", _ => "st-upcoming" };
        public string StatusIcon => Status switch { "done" => "✓", "ongoing" => "●", "cancelled" => "✕", _ => "○" };


        /// <summary>
        /// Chuỗi thuộc tính data-* để gắn lên nút mở modal (chi tiết / sửa / nhân bản / xóa). Mọi giá trị đã được HTML-encode.
        /// time.js đọc các data-* này để điền modal; View in bằng @Html.Raw(e.Attrs(...)).
        /// </summary>
        public string Attrs(string modalId, string mode, string title = "")
        {
            static string A(string key, string? value) => $" data-{key}=\"{System.Net.WebUtility.HtmlEncode(value ?? "")}\"";
            return A("open", modalId) + A("mode", mode) + A("title", title) + A("id", Id.ToString())
                 + A("evtitle", Title) + A("sourcelabel", SourceLabel) + A("origin", OriginLabel) + A("originurl", OriginUrl)
                 + A("editable", Editable ? "true" : "false") + A("datelabel", DateLabel) + A("timerange", TimeRange)
                 + A("location", Location) + A("description", Description) + A("reminder", Reminder)
                 + A("recurrencelabel", Recurrence) + A("recurrence", RecurrenceKey) + A("statuslabel", StatusLabel)
                 + A("eventtype", TypeLabel) + A("color", Color)
                 + A("startdate", Start.ToString("yyyy-MM-dd")) + A("starttime", Start.ToString("HH:mm")) + A("endtime", End.ToString("HH:mm"));
        }

        // Vị trí trên lưới giờ (phần trăm chiều cao cột) và làn khi các sự kiện trùng giờ
        public double TopPct { get; set; }
        public double HeightPct { get; set; }
        public int Lane { get; set; }
        public int Lanes { get; set; } = 1;
    }

    public class TimeSlice
    {
        public string Label { get; set; } = "";
        public string Color { get; set; } = "";
        public int Minutes { get; set; }
        public int Percent { get; set; }
        public double Offset { get; set; }                    // vị trí bắt đầu trên vòng tròn (0 – 100)
        public string MinutesText { get; set; } = "";
    }

    // ------------------------------------------------------------ TỔNG QUAN
    public class TimeOverviewViewModel
    {
        public string DateIso { get; set; } = "";
        public string DateLabel { get; set; } = "";
        public string TodayIso { get; set; } = "";
        public List<TimeEventItem> TodayEvents { get; set; } = new();
        public string PlannedText { get; set; } = "0p";
        public int DoneCount { get; set; }
        public int TotalCount { get; set; }
        public string FocusText { get; set; } = "0p";
        public int FocusSessions { get; set; }
        public List<TimeSlice> Allocation { get; set; } = new();
        public string AllocationTotalText { get; set; } = "0p";
        public string WeekLabel { get; set; } = "";
        public string WeekPlannedText { get; set; } = "0p";
        public int WeekDone { get; set; }
        public int WeekRemaining { get; set; }
        public int WeekPercent { get; set; }
    }

    // ------------------------------------------------------------ LỊCH CÁ NHÂN
    public class CalendarDay
    {
        public string Iso { get; set; } = "";
        public string Dow { get; set; } = "";                 // T2..CN
        public int Day { get; set; }
        public bool IsToday { get; set; }
        public bool InMonth { get; set; } = true;
        public List<TimeEventItem> AllDay { get; set; } = new();
        public List<TimeEventItem> Events { get; set; } = new();
    }
    public class TimeCalendarViewModel
    {
        public string View { get; set; } = "week";            // day / week / month
        public string DateIso { get; set; } = "";
        public string RangeLabel { get; set; } = "";
        public string PrevIso { get; set; } = "";
        public string NextIso { get; set; } = "";
        public string TodayIso { get; set; } = "";
        public int StartHour { get; set; } = 6;
        public int EndHour { get; set; } = 23;
        public List<CalendarDay> Days { get; set; } = new();  // day/week: các cột; month: toàn bộ ô của lưới
        public int EventCount { get; set; }
    }

    // ------------------------------------------------------------ SỰ KIỆN
    public class TimeEventsViewModel
    {
        public string Status { get; set; } = "all";           // all / upcoming / ongoing / done / cancelled
        public string Range { get; set; } = "all";            // all / today / week / month
        public string Type { get; set; } = "all";
        public string Source { get; set; } = "all";
        public string? Q { get; set; }
        public int Total { get; set; }
        public int Upcoming { get; set; }
        public int Done { get; set; }
        public int Cancelled { get; set; }
        public List<TimeEventItem> Items { get; set; } = new();
    }

    // ------------------------------------------------------------ POMODORO
    public class PomodoroTarget { public string Value { get; set; } = ""; public string Label { get; set; } = ""; }
    public class PomodoroSessionHistory
    {
        public int Id { get; set; }
        public string TaskOrCourseName { get; set; } = "";
        public int DurationMinutes { get; set; }
        public string CompletedLabel { get; set; } = "";
    }
    public class TimePomodoroViewModel
    {
        public List<PomodoroTarget> Targets { get; set; } = new();
        public List<PomodoroSessionHistory> History { get; set; } = new();
        public int TodaySessions { get; set; }
        public string TodayText { get; set; } = "0p";
        public int WeekSessions { get; set; }
        public string WeekText { get; set; } = "0p";
        public int TotalSessions { get; set; }
    }
}
