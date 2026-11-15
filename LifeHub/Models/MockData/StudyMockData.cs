using System;
using System.Collections.Generic;
using System.Linq;
using LifeHub.Models.ViewModels.StudyManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho phân hệ Học tập. Các chỉ số (GPA, tiến độ, bài chưa nộp, kỳ thi gần nhất)
    /// đều được TÍNH từ danh sách môn/bài tập, đúng cách back-end sẽ tính bằng truy vấn sau này.
    /// Ngày tháng tính theo hôm nay để trạng thái "quá hạn / sắp tới" luôn đúng.
    /// </summary>
    public static class StudyMockData
    {
        private record ScheduleSlot(int CourseId, DayOfWeek Day, string Start, string End, string Room);

        public static StudyViewModel GetStudyViewModel()
        {
            var today = DateTime.Today;
            var courses = BuildCourses();
            var assignments = BuildAssignments(today, courses);
            UpdateCourseStats(courses, assignments);

            // GPA thang 4 = Σ(điểm quy đổi × tín chỉ) / Σ tín chỉ (chỉ môn đã có điểm)
            var scored = courses.Where(c => c.CurrentScore.HasValue).ToList();
            var scoredCredits = scored.Sum(c => c.Credits);
            decimal? gpa = scoredCredits == 0
                ? (decimal?)null
                : Math.Round(scored.Sum(c => ToGpa4(c.CurrentScore!.Value) * c.Credits) / scoredCredits, 2);

            var nextExam = assignments
                .Where(a => a.IsExam && !a.IsSubmitted && !a.Score.HasValue && a.DueDate.Date >= today)
                .OrderBy(a => a.DueDate)
                .FirstOrDefault();

            return new StudyViewModel
            {
                SemesterName = "HK1 2026-2027",
                TargetGpa = 3.6m,
                CurrentGpa = gpa,
                ActiveCoursesCount = courses.Count(c => c.Status == "Đang học"),
                TotalCredits = courses.Where(c => c.Status == "Đang học").Sum(c => c.Credits),
                PendingAssignmentsCount = assignments.Count(a => !a.IsExam && !a.IsSubmitted && !a.Score.HasValue),
                NextExamDays = nextExam == null ? 0 : (nextExam.DueDate.Date - today).Days,
                NextExamName = nextExam?.CourseName ?? string.Empty,
                Courses = courses,
                Schedule = BuildSchedule(today, courses),
                Assignments = assignments
            };
        }

        private static List<StudyCourseItem> BuildCourses() => new()
        {
            new() { Id = 1, Code = "IT301", Name = "Lập trình Web ASP.NET", Credits = 3, Instructor = "ThS. Nguyễn Minh Khoa", Room = "B2-301", Color = "#2563eb", TargetScore = 9.0m },
            new() { Id = 2, Code = "IT302", Name = "Cơ sở dữ liệu", Credits = 3, Instructor = "TS. Lê Thu Hà", Room = "A1-204", Color = "#7c3aed", TargetScore = 8.5m },
            new() { Id = 3, Code = "MA201", Name = "Xác suất thống kê", Credits = 3, Instructor = "TS. Phạm Quốc Bảo", Room = "C3-105", Color = "#0891b2", TargetScore = 8.0m },
            new() { Id = 4, Code = "EN202", Name = "Tiếng Anh chuyên ngành", Credits = 2, Instructor = "ThS. Trần Ngọc Lan", Room = "B1-102", Color = "#d97706", TargetScore = 8.0m },
            new() { Id = 5, Code = "SE303", Name = "Công nghệ phần mềm", Credits = 3, Instructor = "ThS. Võ Thanh Tùng", Room = "A2-310", Color = "#16a34a", TargetScore = 8.5m }
        };

        private static List<StudyAssignmentItem> BuildAssignments(DateTime today, List<StudyCourseItem> courses)
        {
            StudyAssignmentItem A(int id, int courseId, string title, string kind, int dayOffset, int weight,
                bool submitted = false, decimal? score = null, int hour = 23, int minute = 59,
                int? duration = null, string? location = null)
            {
                var c = courses.First(x => x.Id == courseId);
                return new StudyAssignmentItem
                {
                    Id = id, CourseId = c.Id, CourseCode = c.Code, CourseName = c.Name,
                    Title = title, Kind = kind, WeightPercent = weight,
                    DueDate = today.AddDays(dayOffset).AddHours(hour).AddMinutes(minute),
                    IsSubmitted = submitted || score.HasValue,   // có điểm nghĩa là đã nộp/đã thi
                    Score = score, DurationMinutes = duration, Location = location
                };
            }

            return new List<StudyAssignmentItem>
            {
                A(1, 1, "Bài tập 1: MVC và Razor", "assignment", -20, 10, score: 9.0m),
                A(2, 1, "Quiz Routing và Controller", "quiz", -9, 10, score: 8.0m),
                A(3, 1, "Đồ án giữa kỳ: Web quản lý", "project", 6, 20),

                A(4, 2, "Bài tập chuẩn hóa 3NF", "assignment", -14, 10, score: 8.5m),
                A(5, 2, "Thi giữa kỳ Cơ sở dữ liệu", "midterm", -6, 30, score: 7.5m, hour: 9, minute: 0, duration: 60, location: "A1-204"),
                A(6, 2, "Bài tập truy vấn SQL nâng cao", "assignment", -2, 10),
                A(7, 2, "Thi cuối kỳ Cơ sở dữ liệu", "final", 33, 50, hour: 8, minute: 0, duration: 90, location: "Hội trường A"),

                A(8, 3, "Bài tập chương 3", "assignment", -12, 10, score: 7.0m),
                A(9, 3, "Thi giữa kỳ Xác suất thống kê", "midterm", 5, 30, hour: 13, minute: 0, duration: 90, location: "C3-105"),
                A(10, 3, "Bài tập chương 4", "assignment", 3, 10),

                A(11, 4, "Thuyết trình nhóm", "project", -5, 20, submitted: true),
                A(12, 4, "Quiz từ vựng chuyên ngành", "quiz", 1, 10),

                A(13, 5, "Báo cáo phân tích yêu cầu", "assignment", 8, 15),
                A(14, 5, "Bài tập UML", "assignment", -8, 10, score: 9.5m)
            };
        }

        /// <summary>Tiến độ = số bài đã nộp / tổng số bài. Điểm môn = Σ(điểm quy về thang 10 × trọng số) / Σ trọng số đã chấm.</summary>
        private static void UpdateCourseStats(List<StudyCourseItem> courses, List<StudyAssignmentItem> assignments)
        {
            foreach (var c in courses)
            {
                var list = assignments.Where(a => a.CourseId == c.Id).ToList();
                c.AssignmentProgressPercent = list.Count == 0
                    ? 0
                    : (int)Math.Round(100.0 * list.Count(a => a.IsSubmitted || a.Score.HasValue) / list.Count);

                var graded = list.Where(a => a.Score.HasValue && a.MaxScore > 0).ToList();
                var totalWeight = graded.Sum(a => a.WeightPercent);
                c.CurrentScore = totalWeight == 0
                    ? (decimal?)null
                    : Math.Round(graded.Sum(a => a.Score!.Value / a.MaxScore * 10m * a.WeightPercent) / totalWeight, 2);
            }
        }

        private static List<StudyScheduleDay> BuildSchedule(DateTime today, List<StudyCourseItem> courses)
        {
            var slots = new List<ScheduleSlot>
            {
                new(1, DayOfWeek.Monday,    "07:00", "09:30", "B2-301"),
                new(4, DayOfWeek.Monday,    "13:00", "15:00", "B1-102"),
                new(2, DayOfWeek.Tuesday,   "09:30", "12:00", "A1-204"),
                new(5, DayOfWeek.Tuesday,   "13:00", "15:30", "A2-310"),
                new(3, DayOfWeek.Wednesday, "07:00", "09:30", "C3-105"),
                new(1, DayOfWeek.Thursday,  "13:00", "15:30", "B2-301"),
                new(2, DayOfWeek.Friday,    "07:00", "09:30", "A1-204"),
                new(5, DayOfWeek.Friday,    "13:00", "15:30", "A2-310")
            };

            var days = new (DayOfWeek Day, string Label)[]
            {
                (DayOfWeek.Monday, "T2"), (DayOfWeek.Tuesday, "T3"), (DayOfWeek.Wednesday, "T4"),
                (DayOfWeek.Thursday, "T5"), (DayOfWeek.Friday, "T6"), (DayOfWeek.Saturday, "T7"),
                (DayOfWeek.Sunday, "CN")
            };

            return days.Select(d => new StudyScheduleDay
            {
                DayName = d.Label,
                IsToday = today.DayOfWeek == d.Day,
                Events = slots.Where(s => s.Day == d.Day)
                    .OrderBy(s => s.Start)
                    .Select(s =>
                    {
                        var c = courses.First(x => x.Id == s.CourseId);
                        return new StudyScheduleEvent { Subject = c.Name, Time = $"{s.Start} - {s.End}", Room = s.Room, Color = c.Color };
                    }).ToList()
            }).ToList();
        }

        /// <summary>Quy đổi điểm thang 10 sang thang 4.</summary>
        private static decimal ToGpa4(decimal s) => s switch
        {
            >= 8.5m => 4.0m, >= 8.0m => 3.5m, >= 7.0m => 3.0m, >= 6.5m => 2.5m,
            >= 5.5m => 2.0m, >= 5.0m => 1.5m, >= 4.0m => 1.0m, _ => 0m
        };
    }
}