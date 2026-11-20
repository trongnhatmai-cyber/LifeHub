using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.StudyManagement
{
    // Giữ nguyên tên lớp và thuộc tính cũ. Phần thêm mới được đánh dấu [MỚI].
    public class StudyViewModel
    {
        public string SemesterName { get; set; } = string.Empty;   // [MỚI] "HK1 2026-2027"
        public decimal? TargetGpa { get; set; }                    // [MỚI] GPA mục tiêu (thang 4)

        public decimal? CurrentGpa { get; set; }                   // thang 4
        public int ActiveCoursesCount { get; set; }
        public int TotalCredits { get; set; }
        public int PendingAssignmentsCount { get; set; }
        public int NextExamDays { get; set; }
        public string NextExamName { get; set; } = string.Empty;
        public bool HasNextExam => !string.IsNullOrEmpty(NextExamName);   // [MỚI]

        public List<StudySemesterItem> Semesters { get; set; } = new();   // [MỚI] cho ô chọn học kỳ trong form

        // Danh sách môn học
        public List<StudyCourseItem> Courses { get; set; } = new();

        // Thời khóa biểu tuần
        public List<StudyScheduleDay> Schedule { get; set; } = new();

        // Bài tập & lịch thi
        public List<StudyAssignmentItem> Assignments { get; set; } = new();
    }

    public class StudyCourseItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string Instructor { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public int AssignmentProgressPercent { get; set; }
        public decimal? TargetScore { get; set; }
        public string Status { get; set; } = "Đang học";
        public string Color { get; set; } = "#18181b";             // [MỚI] màu nhận diện môn
        public decimal? CurrentScore { get; set; }                  // [MỚI] điểm hiện tại, thang 10
        public int? SemesterId { get; set; }                        // [MỚI] học kỳ của môn
    }

    public class StudyScheduleDay
    {
        public string DayName { get; set; } = string.Empty;        // T2, T3, T4, T5, T6, T7, CN
        public bool IsToday { get; set; }                           // [MỚI]
        public List<StudyScheduleEvent> Events { get; set; } = new();
    }

    public class StudyScheduleEvent
    {
        public string Subject { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string Color { get; set; } = "#18181b";             // [MỚI]
    }

    public class StudyAssignmentItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int WeightPercent { get; set; }
        public bool IsSubmitted { get; set; }
        public decimal? Score { get; set; }

        public int CourseId { get; set; }                           // [MỚI]
        public string CourseCode { get; set; } = string.Empty;      // [MỚI]
        public string Kind { get; set; } = "assignment";            // [MỚI] assignment / quiz / project / midterm / final
        public decimal MaxScore { get; set; } = 10;                 // [MỚI]
        public int? DurationMinutes { get; set; }                   // [MỚI] bài thi
        public string? Location { get; set; }                       // [MỚI] phòng thi

        public bool IsExam => Kind is "midterm" or "final";         // [MỚI]
        public string KindLabel => Kind switch                      // [MỚI]
        {
            "quiz" => "Kiểm tra nhanh", "project" => "Đồ án", "midterm" => "Thi giữa kỳ", "final" => "Thi cuối kỳ", _ => "Bài tập"
        };

        /// <summary>[MỚI] graded / submitted / overdue / upcoming</summary>
        public string StatusKey(DateTime today) =>
            Score.HasValue ? "graded" : IsSubmitted ? "submitted" : DueDate.Date < today.Date ? "overdue" : "upcoming";

        public string StatusLabel(DateTime today) => StatusKey(today) switch
        {
            "graded" => "Đã chấm điểm", "submitted" => "Đã nộp", "overdue" => "Quá hạn", _ => IsExam ? "Sắp thi" : "Chưa nộp"
        };
    }
}