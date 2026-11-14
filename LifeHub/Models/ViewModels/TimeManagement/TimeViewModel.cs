using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.TimeManagement
{
    public class TimeViewModel
    {
        public int TodayEventsCount { get; set; } = 3;
        public int PomodoroCompletedCount { get; set; } = 5; // 5 phiên = 125 phút
        public int TotalFocusMinutes { get; set; } = 125;
        public int UpcomingDeadlinesCount { get; set; } = 4;

        public List<TimeCalendarEvent> Events { get; set; } = new();
        public List<PomodoroSessionHistory> PomodoroHistory { get; set; } = new();
    }

    public class TimeCalendarEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = "Học tập"; // Học tập, Deadline, Dự án, Cá nhân
        public bool IsAllDay { get; set; }
    }

    public class PomodoroSessionHistory
    {
        public int Id { get; set; }
        public string TaskOrCourseName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 25;
        public DateTime CompletedAt { get; set; }
        public bool IsCompleted { get; set; } = true;
    }
}
