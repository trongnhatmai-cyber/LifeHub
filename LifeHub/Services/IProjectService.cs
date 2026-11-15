using System;
using LifeHub.Models.ViewModels.ProjectManagement;

namespace LifeHub.Services
{
    public class ProjectActionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int? Id { get; init; }

        public static ProjectActionResult Ok(string message, int? id = null) =>
            new() { Success = true, Message = message, Id = id };

        public static ProjectActionResult Fail(string message) =>
            new() { Success = false, Message = message };
    }

    /// <summary>
    /// Nguồn dữ liệu cho phân hệ Dự án. Hiện dùng InMemoryProjectService (bộ nhớ),
    /// sau này viết EfProjectService cùng interface này, View và Controller không phải sửa.
    /// </summary>
    public interface IProjectService
    {
        ProjectViewModel GetIndex(int userId);
        ProjectDetailViewModel? GetDetail(int projectId, int userId);

        ProjectActionResult CreateProject(int userId, string? name, string? description, string? key, DateTime startDate, DateTime dueDate, string? status);
        ProjectActionResult UpdateProjectStatus(int userId, int projectId, string? status);

        ProjectActionResult AddMilestone(int userId, int projectId, string? title, DateTime dueDate);
        ProjectActionResult ToggleMilestone(int userId, int milestoneId);

        ProjectActionResult CreateTask(int userId, int projectId, string? title, string? description, string? priority,
            DateTime? dueDate, int? assigneeId, string? status, int? parentTaskId);
        ProjectActionResult MoveTask(int userId, int taskId, string? status);
        ProjectActionResult AssignTask(int userId, int taskId, int? assigneeId);
        ProjectActionResult ToggleSubtask(int userId, int taskId);
        ProjectActionResult DeleteTask(int userId, int taskId);

        ProjectActionResult InviteMember(int userId, int projectId, string? email, string? role);
        ProjectActionResult CancelInvitation(int userId, int invitationId);
        ProjectActionResult AcceptInvitation(int invitationId);
        ProjectActionResult RemoveMember(int userId, int projectId, int memberUserId);
    }
}