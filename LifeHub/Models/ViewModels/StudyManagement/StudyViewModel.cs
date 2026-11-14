using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.StudyManagement
{
    public class StudyViewModel
    {
        public decimal CurrentGpa { get; set; } 
        public int ActiveCoursesCount { get; set; } 
        public int TotalCredits { get; set; } 
        public int PendingAssignmentsCount { get; set; } 
        public int NextExamDays { get; set; } 
        public string NextExamName { get; set; } 

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
    }

    public class StudyScheduleDay
    {
        public string DayName { get; set; } = string.Empty; // T2, T3, T4, T5, T6, T7, CN
        public List<StudyScheduleEvent> Events { get; set; } = new();
    }

    public class StudyScheduleEvent
    {
        public string Subject { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
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
    }
}
