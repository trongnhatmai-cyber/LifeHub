using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.TaskManagement
{
    public class TaskViewModel
    {
        public int TotalTasks { get; set; } = 12;
        public int TodoTasksCount { get; set; } = 4;
        public int InProgressTasksCount { get; set; } = 5;
        public int CompletedTasksCount { get; set; } = 3;

        public List<TaskItemViewModel> Tasks { get; set; } = new();
    }

    public class TaskItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "todo"; // todo, in_progress, review, done
        public string Priority { get; set; } = "Medium"; // Urgent, High, Medium, Low
        public DateTime? Deadline { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = "Cá nhân";
    }
}
