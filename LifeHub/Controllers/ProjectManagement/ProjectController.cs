using LifeHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class ProjectController : Controller
    {
        // TODO: thay bằng Id của người dùng đang đăng nhập khi có Authentication
        private const int CurrentUserId = 1;

        private readonly IProjectService _projects;

        public ProjectController(IProjectService projects)
        {
            _projects = projects;
        }

        // ---------------- XEM ----------------
        public IActionResult Index()
        {
            ViewData["Title"] = "Dự án";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Project";

            return View(_projects.GetIndex(CurrentUserId));
        }

        public IActionResult Details(int id)
        {
            var model = _projects.GetDetail(id, CurrentUserId);
            if (model == null)
            {
                SetFlash(ProjectActionResult.Fail("Không tìm thấy dự án hoặc bạn chưa tham gia dự án này."));
                return RedirectToAction(nameof(Index));
            }

            ViewData["ActivePage"] = "Project";
            return View(model);
        }

        //Các chức năng gọi trong View
        // ---------------- DỰ ÁN ----------------
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(string? name, string? description, string? key,
            DateTime startDate, DateTime dueDate, string? status)
        {
            var result = _projects.CreateProject(CurrentUserId, name, description, key, startDate, dueDate, status);
            if (result.Success && result.Id.HasValue) return BackToProject(result, result.Id.Value);

            SetFlash(result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int projectId, string? status) =>
            BackToProject(_projects.UpdateProjectStatus(CurrentUserId, projectId, status), projectId);

        // ---------------- CỘT MỐC ----------------
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddMilestone(int projectId, string? title, DateTime dueDate, string? returnTo) =>
            BackByReturn(_projects.AddMilestone(CurrentUserId, projectId, title, dueDate), projectId, returnTo);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleMilestone(int milestoneId, int projectId, string? returnTo) =>
            BackByReturn(_projects.ToggleMilestone(CurrentUserId, milestoneId), projectId, returnTo);

        // ---------------- NHIỆM VỤ ----------------
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateTask(int projectId, string? title, string? description, string? priority,
            DateTime? dueDate, int? assigneeId, string? status, int? parentTaskId, int? openTask) =>
            BackToProject(
                _projects.CreateTask(CurrentUserId, projectId, title, description, priority, dueDate, assigneeId, status, parentTaskId),
                projectId, openTask ?? parentTaskId);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult MoveTask(int projectId, int taskId, string? status, int? openTask) =>
            BackToProject(_projects.MoveTask(CurrentUserId, taskId, status), projectId, openTask);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AssignTask(int projectId, int taskId, int? assigneeId, int? openTask) =>
            BackToProject(_projects.AssignTask(CurrentUserId, taskId, assigneeId), projectId, openTask);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleSubtask(int projectId, int taskId, int? openTask) =>
            BackToProject(_projects.ToggleSubtask(CurrentUserId, taskId), projectId, openTask);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteTask(int projectId, int taskId) =>
            BackToProject(_projects.DeleteTask(CurrentUserId, taskId), projectId);

        // ---------------- THÀNH VIÊN ----------------
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult InviteMember(int projectId, string? email, string? role) =>
            BackToProject(_projects.InviteMember(CurrentUserId, projectId, email, role), projectId);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelInvitation(int projectId, int invitationId) =>
            BackToProject(_projects.CancelInvitation(CurrentUserId, invitationId), projectId);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AcceptInvitation(int projectId, int invitationId) =>
            BackToProject(_projects.AcceptInvitation(invitationId), projectId);

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveMember(int projectId, int userId) =>
            BackToProject(_projects.RemoveMember(CurrentUserId, projectId, userId), projectId);

        // ---------------- HỖ TRỢ ----------------
        private void SetFlash(ProjectActionResult result)
        {
            TempData["ProjectMessage"] = result.Message;
            TempData["ProjectMessageType"] = result.Success ? "success" : "error";
        }

        private IActionResult BackToProject(ProjectActionResult result, int projectId, int? openTask = null)
        {
            SetFlash(result);
            return RedirectToAction(nameof(Details), "Project", new { id = projectId },
                openTask.HasValue ? $"task-{openTask.Value}" : null);
        }

        private IActionResult BackByReturn(ProjectActionResult result, int projectId, string? returnTo)
        {
            SetFlash(result);
            return returnTo == "details"
                ? RedirectToAction(nameof(Details), new { id = projectId })
                : RedirectToAction(nameof(Index));
        }
    }
}