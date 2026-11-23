namespace LifeHub.Models.ViewModels;

public class ReportPageViewModel
{
    public string Section { get; set; } = "overview";
    public string Period { get; set; } = "month";
    public string PeriodLabel { get; set; } = string.Empty;
    public string ActionName { get; set; } = "Index";
    public DateOnly? FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int CourseCount { get; set; }
    public int CompletedCourseCount { get; set; }
    public int GradedAssignmentCount { get; set; }
    public decimal? AverageCourseScore { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OpenTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int TaskCompletionPercent { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal WalletBalance { get; set; }
    public int TransactionCount { get; set; }
    public int ActiveGoalCount { get; set; }
    public int CompletedGoalCount { get; set; }
    public int ActiveHabitCount { get; set; }
    public int CompletedHabitLogCount { get; set; }
    public int HabitLogCount { get; set; }
    public int FocusMinutes { get; set; }
}
