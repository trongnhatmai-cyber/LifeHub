using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace LifeHub.Models.ViewModels.StudyManagement
{
    // ===================== MÔN HỌC =====================
    public class StudyCourseRow
    {
        public StudyCourseItem Course { get; set; } = new();
        public string SemesterName { get; set; } = string.Empty;
        public int? SemesterId { get; set; }   // [MỚI]
        public int AssignmentCount { get; set; }
        public int SubmittedCount { get; set; }
        public string ScheduleSummary { get; set; } = string.Empty;
        public bool IsCompleted => Course.Status == "Đã hoàn thành";
    }

    public class StudyCoursesPageViewModel
    {
        /// <summary>current / completed / all</summary>
        public string Status { get; set; } = "current";
        public List<StudyCourseRow> Rows { get; set; } = new();
        public int CurrentCount { get; set; }
        public int CompletedCount { get; set; }
        public int CurrentCredits { get; set; }
        public decimal? AverageScore { get; set; }

        // [MỚI] Lọc theo học kỳ và danh sách học kỳ cho ô chọn trong form
        public int? SemesterId { get; set; }
        public string SemesterFilterName { get; set; } = string.Empty;
        public List<StudySemesterItem> Semesters { get; set; } = new();
    }

    // ===================== HỌC KỲ [MỚI] =====================
    /// <summary>Một học kỳ (bảng semesters). Tên thuộc tính khớp entity Semester.</summary>
    public class StudySemesterItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsCurrent { get; set; }

        /// <summary>current / finished / upcoming</summary>
        public string StatusKey(DateOnly today) =>
            IsCurrent ? "current" : EndDate.HasValue && EndDate.Value < today ? "finished" : "upcoming";

        public string DateRange =>
            (StartDate?.ToString("dd/MM/yyyy") ?? "—") + " đến " + (EndDate?.ToString("dd/MM/yyyy") ?? "—");
    }

    public class StudySemesterRow
    {
        public StudySemesterItem Semester { get; set; } = new();
        public string StatusKey { get; set; } = "upcoming";
        public string StatusLabel { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public int Credits { get; set; }
        public decimal? Gpa4 { get; set; }
        public int TimeProgressPercent { get; set; }
        public int? DaysLeft { get; set; }
        public List<StudyCourseItem> Courses { get; set; } = new();
        public bool IsCurrent => Semester.IsCurrent;
    }

    public class StudySemestersPageViewModel
    {
        public List<StudySemesterRow> Rows { get; set; } = new();
        public StudySemesterRow? Current => Rows.FirstOrDefault(r => r.IsCurrent);
        public decimal? CumulativeGpa { get; set; }
        public int CumulativeCredits { get; set; }
        public int FinishedCount => Rows.Count(r => r.StatusKey == "finished");
        public int UpcomingCount => Rows.Count(r => r.StatusKey == "upcoming");
    }

    // ===================== THỜI KHÓA BIỂU =====================
    public class StudyScheduleSlot
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public DayOfWeek Day { get; set; }
        public string Start { get; set; } = "07:00";
        public string End { get; set; } = "09:00";
        public string Room { get; set; } = string.Empty;

        // Do trang Thời khóa biểu điền khi dựng dữ liệu
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Color { get; set; } = "#2563eb";
        public bool HasConflict { get; set; }

        public int StartMinutes => ToMinutes(Start);
        public int EndMinutes => ToMinutes(End);
        public int DayIndex => ((int)Day + 6) % 7;   // 0 = Thứ Hai ... 6 = Chủ nhật
        public string DayLabel => Day switch
        {
            DayOfWeek.Monday => "T2", DayOfWeek.Tuesday => "T3", DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5", DayOfWeek.Friday => "T6", DayOfWeek.Saturday => "T7", _ => "CN"
        };

        private static int ToMinutes(string hhmm) => int.Parse(hhmm[..2]) * 60 + int.Parse(hhmm[3..5]);
    }

    public class StudyTimetableBlock
    {
        public StudyScheduleSlot Slot { get; set; } = new();
        public double TopPercent { get; set; }
        public double HeightPercent { get; set; }
        public string TopCss => TopPercent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        public string HeightCss => HeightPercent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }

    public class StudyTimetableDay
    {
        public string DayName { get; set; } = string.Empty;
        public bool IsToday { get; set; }
        public List<StudyTimetableBlock> Blocks { get; set; } = new();
    }

    public class StudySchedulePageViewModel
    {
        public int StartHour { get; set; } = 7;
        public int EndHour { get; set; } = 18;
        public List<StudyTimetableDay> Days { get; set; } = new();
        public List<StudyScheduleSlot> Slots { get; set; } = new();
        public List<StudyCourseItem> Courses { get; set; } = new();
        public int TodayCount { get; set; }
        public double WeeklyHours { get; set; }
        public int ConflictCount => Slots.Count(s => s.HasConflict);
    }

    // ===================== BÀI TẬP VÀ KỲ THI =====================
    public class StudyAssignmentsPageViewModel
    {
        public string Status { get; set; } = "all";   // all / upcoming / overdue / done
        public string Kind { get; set; } = "all";     // all / assignment / quiz / project / midterm / final
        public int? CourseId { get; set; }
        public string? Q { get; set; }
        public DateTime Today { get; set; } = DateTime.Today;
        public List<StudyAssignmentItem> Items { get; set; } = new();
        public List<StudyCourseItem> Courses { get; set; } = new();

        // Đếm trên TOÀN BỘ bài tập, không phụ thuộc bộ lọc.
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public int DoneCount { get; set; }

        public bool HasFilter => Status != "all" || Kind != "all" || CourseId.HasValue || !string.IsNullOrWhiteSpace(Q);

        public string DueHint(StudyAssignmentItem a)
        {
            if (a.Score.HasValue || a.IsSubmitted) return string.Empty;
            var days = (a.DueDate.Date - Today.Date).Days;
            return days < 0 ? $"Quá hạn {-days} ngày" : days == 0 ? "Hạn hôm nay" : $"Còn {days} ngày";
        }
    }

    // ===================== ĐIỂM SỐ =====================
    public class StudyGradeItem
    {
        public string Title { get; set; } = string.Empty;
        public string KindLabel { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; } = 10;
        public int WeightPercent { get; set; }
    }

    public class StudyGradeRow
    {
        public StudyCourseItem Course { get; set; } = new();
        public List<StudyGradeItem> Graded { get; set; } = new();
        public int GradedWeight { get; set; }
        public int RemainingWeight { get; set; }
        public decimal? Gpa4 { get; set; }
        public string Letter { get; set; } = "—";
        public decimal? Gap { get; set; }
        public string RequiredLabel { get; set; } = string.Empty;
        public bool MetTarget => Gap.HasValue && Gap.Value >= 0;
    }

    public class StudyGradesPageViewModel
    {
        public decimal? Gpa { get; set; }
        public decimal? TargetGpa { get; set; }
        public int ScoredCredits { get; set; }
        public int TargetMetCount { get; set; }
        public List<StudyGradeRow> Rows { get; set; } = new();
    }

    // ===================== TÀI LIỆU HỌC TẬP =====================
    public class StudyDocumentItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseColor { get; set; } = "#2563eb";
        /// <summary>pdf / slide / link / video / note</summary>
        public string Kind { get; set; } = "pdf";
        public string Url { get; set; } = string.Empty;
        public string SizeLabel { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public string KindLabel => Kind switch
        {
            "pdf" => "PDF", "slide" => "Slide", "video" => "Video", "note" => "Ghi chú", _ => "Liên kết"
        };

        /// <summary>link = liên kết ngoài, file = tệp tải lên từ máy, content = nội dung soạn trong web</summary>
        public string Source { get; set; } = "link";
        public string SourceLabel => Source switch { "file" => "Tệp tải lên", "content" => "Soạn trong web", _ => "Liên kết" };
    }

    public class StudyDocumentsPageViewModel
    {
        public int? CourseId { get; set; }
        public string Kind { get; set; } = "all";
        public string? Q { get; set; }
        public List<StudyDocumentItem> Items { get; set; } = new();
        public List<StudyCourseItem> Courses { get; set; } = new();
        public int TotalCount { get; set; }
        public bool HasFilter => CourseId.HasValue || Kind != "all" || !string.IsNullOrWhiteSpace(Q);
    }

    // ===================== DỮ LIỆU FORM =====================
    public class UpdateCourseInput : CreateCourseInput
    {
        public int Id { get; set; }
        /// <summary>Đang học / Đã hoàn thành</summary>
        public string Status { get; set; } = "Đang học";
    }

    /// <summary>Thêm hoặc sửa một buổi học lặp lại hàng tuần (bảng course_schedules). Id = 0 khi thêm.</summary>
    public class SaveScheduleSlotInput
    {
        public int Id { get; set; }
        [Required] public int CourseId { get; set; }
        /// <summary>0 = Chủ nhật, 1 = Thứ Hai ... 6 = Thứ Bảy</summary>
        [Range(0, 6)] public int Day { get; set; } = 1;
        [Required] public string Start { get; set; } = "07:00";
        [Required] public string End { get; set; } = "09:00";
        public string? Room { get; set; }
    }

    /// <summary>[MỚI] Thêm hoặc sửa học kỳ (bảng semesters). Id = 0 khi thêm.</summary>
    public class SaveSemesterInput
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required] public DateOnly? StartDate { get; set; }
        [Required] public DateOnly? EndDate { get; set; }
    }

    public class UpdateAssignmentInput : CreateAssignmentInput
    {
        public int Id { get; set; }
    }

    public class RecordScoreInput
    {
        [Required] public int Id { get; set; }
        [Range(0, 1000)] public decimal Score { get; set; }
    }

    public class SetTargetScoreInput
    {
        [Required] public int CourseId { get; set; }
        [Range(0, 10)] public decimal TargetScore { get; set; }
    }

    /// <summary>Thêm tài liệu: chọn tệp từ máy (File) HOẶC nhập đường dẫn (Url), cần ít nhất một trong hai.</summary>
    public class CreateDocumentInput : IValidatableObject
    {
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [Required] public int CourseId { get; set; }
        [Required] public string Kind { get; set; } = "pdf";
        public IFormFile? File { get; set; }                 // tệp chọn từ máy tính
        [StringLength(500)] public string? Url { get; set; } // hoặc liên kết trực tuyến
        /// <summary>Nội dung soạn trực tiếp (HTML từ trình soạn thảo). Server làm sạch rồi lưu thành tệp riêng.</summary>
        [StringLength(60000)] public string? Content { get; set; }
        [StringLength(500)] public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (File == null && string.IsNullOrWhiteSpace(Url) && string.IsNullOrWhiteSpace(Content))
                yield return new ValidationResult("Hãy chọn tệp từ máy, soạn nội dung hoặc nhập đường dẫn.", new[] { nameof(File), nameof(Url), nameof(Content) });
        }
    }

    /// <summary>Sửa tài liệu: giữ tệp/đường dẫn cũ nếu không chọn mới.</summary>
    public class UpdateDocumentInput
    {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [Required] public int CourseId { get; set; }
        [Required] public string Kind { get; set; } = "pdf";
        public IFormFile? File { get; set; }
        [StringLength(500)] public string? Url { get; set; }
        [StringLength(500)] public string? Note { get; set; }
    }

    /// <summary>Trang đọc nội dung tài liệu được soạn trong web (Study/ViewDocument).</summary>
    public class StudyDocumentContentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseColor { get; set; } = "#2563eb";
        /// <summary>VD: "Bài tập: Báo cáo tuần 3" khi nội dung thuộc một bài tập.</summary>
        public string ContextLabel { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;   // đã làm sạch ở server
        public DateTime UpdatedAt { get; set; }
    }
}
