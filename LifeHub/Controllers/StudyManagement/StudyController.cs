using LifeHub.Models.MockData;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class StudyController : Controller
    {
        // GET /Study
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý học tập";
            ViewData["ActivePage"] = "Study";

            // TODO(backend): thay bằng service đọc Database, giữ nguyên StudyViewModel.
            return View(StudyMockData.GetStudyViewModel());
        }

        // POST /Study/CreateCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourse()
        {
            return StatusCode(
                StatusCodes.Status501NotImplemented,
                "Chức năng thêm môn học chưa được triển khai."
            );
        }

        // POST /Study/CreateAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAssignment()
        {
            return StatusCode(
                StatusCodes.Status501NotImplemented,
                "Chức năng thêm bài tập chưa được triển khai."
            );
        }
    }
}