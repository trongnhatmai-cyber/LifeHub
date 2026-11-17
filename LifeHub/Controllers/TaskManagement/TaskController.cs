using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.TaskManagement;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class TaskController : Controller
    {
        // GET /Task
        public IActionResult Index()
        {
            ViewData["Title"] = "Công việc";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Task";

            // ĐIỂM THAY THẾ DUY NHẤT: sau này đổi dòng này sang service/DB, View không phải sửa.
            var model = TaskMockData.GetTaskViewModel();

            return View(model);
        }

        // POST /Task/Create  (form "Thêm công việc")
        // TODO (Back-end): kiểm tra ModelState, lưu bảng tasks (+ tags, task_tags), rồi RedirectToAction(nameof(Index)).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateTaskInput input)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Chức năng thêm công việc chưa được triển khai.");
        }

        // POST /Task/UpdateStatus  (kéo thả thẻ sang cột khác trên Kanban)
        // TODO (Back-end): cập nhật tasks.Status, tasks.CompletedAt (khi chuyển sang done) và trả về Ok().
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(UpdateTaskStatusInput input)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Chức năng cập nhật trạng thái chưa được triển khai.");
        }

        // POST /Task/AddComment  (khung "Bình luận & trao đổi" trong chi tiết công việc)
        // TODO (Back-end): lưu task_comments; nếu việc thuộc dự án thì ghi thêm project_activities
        // và tạo notifications cho người được giao.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(AddCommentInput input)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Chức năng bình luận chưa được triển khai.");
        }
    }
}