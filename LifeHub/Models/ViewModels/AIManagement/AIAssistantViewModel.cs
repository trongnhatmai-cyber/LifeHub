using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.AIManagement
{
    /// <summary>Dữ liệu tổng hợp cho trang Trợ lý AI (Views/AI/Index.cshtml).</summary>
    public class AIAssistantViewModel
    {
        public string UserName { get; set; } = "Bạn";
        public int PendingTasks { get; set; }
        public int HighPriorityTasks { get; set; }
        public int UpcomingDeadlines { get; set; }
        public int TodayEvents { get; set; }
        public int FocusMinutesToday { get; set; }
        public int ActiveGoals { get; set; }
        public int ActiveHabits { get; set; }
        public decimal MonthExpense { get; set; }
        public decimal MonthIncome { get; set; }

        public List<AIInsightItem> Insights { get; set; } = new();
        public List<AIPrioritySuggestion> TopPriorities { get; set; } = new();
    }

    public class AIInsightItem
    {
        public string Badge { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
    }

    public class AIPrioritySuggestion
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium";
        public string DeadlineLabel { get; set; } = string.Empty;
    }

    public class AIAskInput
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
