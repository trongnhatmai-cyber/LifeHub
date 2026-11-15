using LifeHub.Models.MockData;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    // Chống giả mạo form cho mọi action POST (form trong View đã tự có token).
    [AutoValidateAntiforgeryToken]
    public class ProjectController : Controller
    {
        // GET /Project
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Dự án";
            ViewData["ActivePage"] = "Project";

            // TODO(backend): thay bằng service đọc Database, giữ nguyên ProjectViewModel.
            return View(ProjectMockData.BuildIndex());
        }

        // GET /Project/Details/3
        [HttpGet]
        public IActionResult Details(int id)
        {
            var model = ProjectMockData.BuildDetail(id);
            if (model is null) return NotFound();

            return View(model);
        }

        // =====================================================================
        // CÁC ACTION POST: tên action và tên tham số trùng với form trong View.
        // Hiện chỉ là bản giữ chỗ: báo "chưa triển khai" rồi quay lại trang cũ.
        // TODO(backend): làm từng action, sau đó thay NotYet(...) bằng logic thật.
        // =====================================================================

        [HttpPost]
        public IActionResult Create(string name, string? key, string? description,
            DateTime? startDate, DateTime? dueDate, string? status)
            => NotYet("Tạo dự án");

        [HttpPost]
        public IActionResult UpdateStatus(int projectId, string status)
            => NotYet("Đổi trạng thái dự án", projectId);

        [HttpPost]
        public IActionResult InviteMember(int projectId, string email, string role)
            => NotYet("Mời thành viên", projectId);

        [HttpPost]
        public IActionResult AcceptInvitation(int projectId, int invitationId)
            => NotYet("Chấp nhận lời mời", projectId);

        [HttpPost]
        public IActionResult CancelInvitation(int projectId, int invitationId)
            => NotYet("Hủy lời mời", projectId);

        [HttpPost]
        public IActionResult RemoveMember(int projectId, int userId)
            => NotYet("Xóa thành viên", projectId);

        [HttpPost]
        public IActionResult CreateTask(int projectId, string title, string? description, string? status,
            string? priority, DateTime? dueDate, int? assigneeId, int? parentTaskId)
            => NotYet("Tạo nhiệm vụ", projectId, openTask: parentTaskId);

        [HttpPost]
        public IActionResult MoveTask(int projectId, int taskId, string status, int? openTask)
            => NotYet("Chuyển cột nhiệm vụ", projectId, openTask: openTask);

        [HttpPost]
        public IActionResult AssignTask(int projectId, int taskId, int? assigneeId, int? openTask)
            => NotYet("Giao nhiệm vụ", projectId, openTask: openTask);

        [HttpPost]
        public IActionResult ToggleSubtask(int projectId, int taskId, int? openTask)
            => NotYet("Đánh dấu việc con", projectId, openTask: openTask);

        [HttpPost]
        public IActionResult DeleteTask(int projectId, int taskId)
            => NotYet("Xóa nhiệm vụ", projectId);

        [HttpPost]
        public IActionResult AddMilestone(int projectId, string title, DateTime dueDate, string? returnTo)
            => NotYet("Thêm cột mốc", projectId, returnTo);

        [HttpPost]
        public IActionResult ToggleMilestone(int milestoneId, int projectId, string? returnTo)
            => NotYet("Đánh dấu cột mốc", projectId, returnTo);

        /// <summary>Báo "chưa triển khai" qua _Flash rồi quay về trang Index hoặc Details (mở lại nhiệm vụ nếu có).</summary>
        private IActionResult NotYet(string feature, int? projectId = null, string? returnTo = null, int? openTask = null)
        {
            TempData["ProjectMessage"] = $"{feature}: bản mô phỏng, chức năng này chưa được triển khai nên dữ liệu chưa được lưu.";
            TempData["ProjectMessageType"] = "info";

            if (projectId is null || returnTo == "index")
                return RedirectToAction(nameof(Index));

            return RedirectToAction(nameof(Details), "Project", new { id = projectId },
                openTask is null ? null : $"task-{openTask}");
        }
    }
}