using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LifeHub.Models.ViewModels.StudyManagement;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Dữ liệu mẫu cho các trang con của Học tập (Môn học, Thời khóa biểu, Bài tập, Điểm số, Tài liệu).
    /// Dựa trên StudyMockData nên số liệu luôn khớp với trang Tổng quan. Mọi lọc và tính toán làm ở C#.
    /// </summary>
    public static class StudyPagesMockData
    {
        // ---------------------------------------------------------------- Môn học
        public static StudyCoursesPageViewModel GetCoursesPage(string? status)
        {
            status = (status is "completed" or "all") ? status : "current";
            var vm = StudyMockData.GetStudyViewModel();
            var slots = StudyMockData.GetScheduleSlots();

            // Hai môn đã hoàn thành ở học kỳ trước (không tính vào GPA học kỳ này ở trang Tổng quan).
            var finished = new List<StudyCourseItem>
            {
                new() { Id = 6, Code = "IT201", Name = "Cấu trúc dữ liệu", Credits = 3, Instructor = "TS. Hoàng Gia Huy", Room = "A1-105", Color = "#0ea5e9", Status = "Đã hoàn thành", CurrentScore = 8.4m, TargetScore = 8.0m, AssignmentProgressPercent = 100 },
                new() { Id = 7, Code = "PH101", Name = "Vật lý đại cương", Credits = 3, Instructor = "ThS. Đặng Minh Châu", Room = "C1-201", Color = "#f43f5e", Status = "Đã hoàn thành", CurrentScore = 7.2m, TargetScore = 7.5m, AssignmentProgressPercent = 100 }
            };

            StudyCourseRow Row(StudyCourseItem c, string semester)
            {
                var list = vm.Assignments.Where(a => a.CourseId == c.Id).ToList();
                var when = slots.Where(s => s.CourseId == c.Id).OrderBy(s => s.DayIndex).ThenBy(s => s.Start).Select(s => $"{s.DayLabel} {s.Start}");
                return new StudyCourseRow
                {
                    Course = c, SemesterName = semester,
                    AssignmentCount = list.Count,
                    SubmittedCount = list.Count(a => a.IsSubmitted || a.Score.HasValue),
                    ScheduleSummary = string.Join(" · ", when)
                };
            }

            var all = vm.Courses.Select(c => Row(c, vm.SemesterName))
                .Concat(finished.Select(c => Row(c, "HK2 2025-2026"))).ToList();
            var current = all.Where(r => !r.IsCompleted).ToList();
            var scored = current.Where(r => r.Course.CurrentScore.HasValue).ToList();

            return new StudyCoursesPageViewModel
            {
                Status = status,
                Rows = all.Where(r => status == "all" || (status == "completed" ? r.IsCompleted : !r.IsCompleted)).ToList(),
                CurrentCount = current.Count,
                CompletedCount = all.Count - current.Count,
                CurrentCredits = current.Sum(r => r.Course.Credits),
                AverageScore = scored.Count == 0 ? null : Math.Round(scored.Average(r => r.Course.CurrentScore!.Value), 2)
            };
        }

        // ---------------------------------------------------------------- Thời khóa biểu
        public static StudySchedulePageViewModel GetSchedulePage()
        {
            var vm = StudyMockData.GetStudyViewModel();
            var slots = StudyMockData.GetScheduleSlots();
            const int startHour = 7, endHour = 18;
            const double span = (endHour - startHour) * 60.0;

            foreach (var s in slots)
            {
                var c = vm.Courses.First(x => x.Id == s.CourseId);
                s.CourseCode = c.Code; s.CourseName = c.Name; s.Color = c.Color;
                s.HasConflict = slots.Any(o => o.Id != s.Id && o.Day == s.Day && o.StartMinutes < s.EndMinutes && s.StartMinutes < o.EndMinutes);
            }

            var order = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            var today = DateTime.Today.DayOfWeek;

            return new StudySchedulePageViewModel
            {
                StartHour = startHour, EndHour = endHour,
                Courses = vm.Courses,
                Slots = slots.OrderBy(s => s.DayIndex).ThenBy(s => s.StartMinutes).ToList(),
                TodayCount = slots.Count(s => s.Day == today),
                WeeklyHours = Math.Round(slots.Sum(s => s.EndMinutes - s.StartMinutes) / 60.0, 1),
                Days = order.Select(d => new StudyTimetableDay
                {
                    DayName = new StudyScheduleSlot { Day = d }.DayLabel,
                    IsToday = d == today,
                    Blocks = slots.Where(s => s.Day == d).OrderBy(s => s.StartMinutes).Select(s => new StudyTimetableBlock
                    {
                        Slot = s,
                        TopPercent = (s.StartMinutes - startHour * 60) / span * 100,
                        HeightPercent = (s.EndMinutes - s.StartMinutes) / span * 100
                    }).ToList()
                }).ToList()
            };
        }

        // ---------------------------------------------------------------- Bài tập và kỳ thi
        public static StudyAssignmentsPageViewModel GetAssignmentsPage(string? status, string? kind, int? courseId, string? q)
        {
            var vm = StudyMockData.GetStudyViewModel();
            var today = DateTime.Today;
            status = (status is "upcoming" or "overdue" or "done") ? status : "all";
            kind = (kind is "assignment" or "quiz" or "project" or "midterm" or "final") ? kind : "all";
            var key = Fold(q);

            IEnumerable<StudyAssignmentItem> items = vm.Assignments;
            if (status == "done") items = items.Where(a => a.StatusKey(today) is "graded" or "submitted");
            else if (status != "all") items = items.Where(a => a.StatusKey(today) == status);
            if (kind != "all") items = items.Where(a => a.Kind == kind);
            if (courseId.HasValue) items = items.Where(a => a.CourseId == courseId.Value);
            if (key.Length > 0) items = items.Where(a => Fold(a.Title).Contains(key) || Fold(a.CourseName).Contains(key));

            return new StudyAssignmentsPageViewModel
            {
                Status = status, Kind = kind, CourseId = courseId, Q = q, Today = today,
                Items = items.OrderBy(a => a.DueDate).ToList(),
                Courses = vm.Courses,
                TotalCount = vm.Assignments.Count,
                PendingCount = vm.Assignments.Count(a => a.StatusKey(today) == "upcoming" && !a.IsExam),
                OverdueCount = vm.Assignments.Count(a => a.StatusKey(today) == "overdue"),
                DoneCount = vm.Assignments.Count(a => a.StatusKey(today) is "graded" or "submitted")
            };
        }

        // ---------------------------------------------------------------- Điểm số
        public static StudyGradesPageViewModel GetGradesPage()
        {
            var vm = StudyMockData.GetStudyViewModel();

            var rows = vm.Courses.Select(c =>
            {
                var list = vm.Assignments.Where(a => a.CourseId == c.Id).ToList();
                var graded = list.Where(a => a.Score.HasValue).ToList();
                var gradedWeight = graded.Sum(a => a.WeightPercent);
                var remaining = list.Where(a => !a.Score.HasValue).Sum(a => a.WeightPercent);
                decimal? gap = c.CurrentScore.HasValue && c.TargetScore.HasValue ? c.CurrentScore - c.TargetScore : null;

                // Điểm trung bình cần có ở các bài còn lại để chạm điểm mục tiêu.
                string required;
                if (!c.TargetScore.HasValue) required = "Chưa đặt điểm mục tiêu";
                else if (remaining == 0) required = "Đã có đủ điểm các bài";
                else
                {
                    var need = (c.TargetScore.Value * (gradedWeight + remaining) - (c.CurrentScore ?? 0m) * gradedWeight) / remaining;
                    required = need <= 0 ? "Đã đạt điểm mục tiêu"
                        : need > 10 ? "Khó đạt mục tiêu với các bài còn lại"
                        : $"Cần trung bình {Math.Round(need, 1).ToString("0.0")} điểm ở các bài còn lại";
                }

                return new StudyGradeRow
                {
                    Course = c, GradedWeight = gradedWeight, RemainingWeight = remaining, Gap = gap, RequiredLabel = required,
                    Gpa4 = c.CurrentScore.HasValue ? StudyMockData.ToGpa4(c.CurrentScore.Value) : null,
                    Letter = c.CurrentScore.HasValue ? Letter(c.CurrentScore.Value) : "—",
                    Graded = graded.Select(a => new StudyGradeItem { Title = a.Title, KindLabel = a.KindLabel, Score = a.Score!.Value, MaxScore = a.MaxScore, WeightPercent = a.WeightPercent }).ToList()
                };
            }).ToList();

            return new StudyGradesPageViewModel
            {
                Gpa = vm.CurrentGpa, TargetGpa = vm.TargetGpa, Rows = rows,
                ScoredCredits = rows.Where(r => r.Course.CurrentScore.HasValue).Sum(r => r.Course.Credits),
                TargetMetCount = rows.Count(r => r.MetTarget)
            };
        }

        private static string Letter(decimal s) => s switch
        {
            >= 8.5m => "A", >= 8.0m => "B+", >= 7.0m => "B", >= 6.5m => "C+", >= 5.5m => "C", >= 5.0m => "D+", >= 4.0m => "D", _ => "F"
        };

        // ---------------------------------------------------------------- Tài liệu học tập
        public static StudyDocumentsPageViewModel GetDocumentsPage(int? courseId, string? kind, string? q)
        {
            var vm = StudyMockData.GetStudyViewModel();
            var today = DateTime.Today;
            kind = (kind is "pdf" or "slide" or "link" or "video" or "note") ? kind : "all";
            var key = Fold(q);

            StudyDocumentItem D(int id, int cid, string title, string k, string size, int daysAgo, string note = "")
            {
                var c = vm.Courses.First(x => x.Id == cid);
                return new StudyDocumentItem
                {
                    Id = id, Title = title, CourseId = c.Id, CourseCode = c.Code, CourseColor = c.Color, Kind = k,
                    SizeLabel = size, Note = note, AddedAt = today.AddDays(-daysAgo),
                    Url = "https://example.com/lifehub/study/" + id   // đường dẫn mẫu
                };
            }

            var all = new List<StudyDocumentItem>
            {
                D(1, 1, "Slide buổi 5: Razor và Tag Helper", "slide", "2,4 MB", 12),
                D(2, 1, "Tài liệu ASP.NET Core chính thức", "link", "", 20, "Mục Controllers và Views"),
                D(3, 2, "Giáo trình Cơ sở dữ liệu (chương 4-6)", "pdf", "8,1 MB", 25),
                D(4, 2, "Bài giảng chuẩn hóa 3NF", "slide", "1,9 MB", 14),
                D(5, 2, "Bộ đề SQL luyện tập", "note", "", 3, "Các câu JOIN, GROUP BY và truy vấn lồng"),
                D(6, 3, "Tổng hợp công thức xác suất", "pdf", "0,6 MB", 6),
                D(7, 3, "Video bài giảng biến ngẫu nhiên", "video", "", 9),
                D(8, 4, "300 từ vựng chuyên ngành", "pdf", "0,3 MB", 4),
                D(9, 5, "Mẫu báo cáo phân tích yêu cầu", "pdf", "0,5 MB", 15)
            };

            IEnumerable<StudyDocumentItem> items = all;
            if (courseId.HasValue) items = items.Where(d => d.CourseId == courseId.Value);
            if (kind != "all") items = items.Where(d => d.Kind == kind);
            if (key.Length > 0) items = items.Where(d => Fold(d.Title).Contains(key) || Fold(d.Note).Contains(key));

            return new StudyDocumentsPageViewModel
            {
                CourseId = courseId, Kind = kind, Q = q, Courses = vm.Courses, TotalCount = all.Count,
                Items = items.OrderByDescending(d => d.AddedAt).ToList()
            };
        }

        // Bỏ dấu tiếng Việt để tìm "doan" ra "Đồ án".
        private static string Fold(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sb = new StringBuilder();
            foreach (var ch in text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Replace('đ', 'd');
        }
    }
}