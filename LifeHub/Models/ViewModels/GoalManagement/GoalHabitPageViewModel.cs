using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.GoalManagement
{
    public class GoalHabitPageViewModel
    {
        public string Eyebrow { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Accent { get; set; } = "goal";
        public string PrimaryActionLabel { get; set; } = "";
        public List<PageMetricViewModel> Metrics { get; set; } = new();
        public List<PageItemViewModel> Items { get; set; } = new();
        public List<ChartPointViewModel> Chart { get; set; } = new();
        public List<string> Filters { get; set; } = new();
        public string SectionTitle { get; set; } = "Danh sách";
        public string SectionDescription { get; set; } = "";
        public string SecondaryTitle { get; set; } = "Thông tin nổi bật";
        public List<PageItemViewModel> SecondaryItems { get; set; } = new();
    }

    public class PageMetricViewModel
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Hint { get; set; } = "";
        public string Icon { get; set; } = "◈";
    }

    public class PageItemViewModel
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Status { get; set; } = "";
        public string Category { get; set; } = "";
        public string Value { get; set; } = "";
        public int Progress { get; set; }
        public string DueDate { get; set; } = "";
    }

    public class ChartPointViewModel
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }
}
