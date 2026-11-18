using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.StudyManagement;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class StudyController : Controller
    {
        private void Page(string title, string activePage)
        {
            ViewData["Title"] = title;
            // Giá trị phải trùng với khóa mục con trong _Layout.cshtml để mục đó được tô sáng.
            ViewData["ActivePage"] = activePage;
        }

        // ĐIỂM THAY THẾ DUY NHẤT của mỗi trang: sau này đổi sang service/DB, View không phải sửa.

        // GET /Study
        public IActionResult Index() { Page("Quản lý học tập", "StudyOverview"); return View(StudyMockData.GetStudyViewModel()); }

        // GET /Study/Courses?status=current|completed|all
        public IActionResult Courses(string? status) { Page("Môn học và khóa học", "Course"); return View(StudyPagesMockData.GetCoursesPage(status)); }

        // GET /Study/Schedule
        public IActionResult Schedule() { Page("Thời khóa biểu", "Schedule"); return View(StudyPagesMockData.GetSchedulePage()); }

        // GET /Study/Assignments?status=upcoming|overdue|done&kind=quiz&courseId=1&q=...
        public IActionResult Assignments(string? status, string? kind, int? courseId, string? q)
        {
            Page("Bài tập và kỳ thi", "Assignment");
            return View(StudyPagesMockData.GetAssignmentsPage(status, kind, courseId, q));
        }

        // GET /Study/Grades
        public IActionResult Grades() { Page("Điểm số", "Grade"); return View(StudyPagesMockData.GetGradesPage()); }

        // GET /Study/Documents?courseId=1&kind=pdf&q=...
        public IActionResult Documents(int? courseId, string? kind, string? q)
        {
            Page("Tài liệu học tập", "StudyDocument");
            return View(StudyPagesMockData.GetDocumentsPage(courseId, kind, q));
        }

        // ------------------------------------------------------------------ POST (chưa triển khai)
        // TODO (Back-end): kiểm tra ModelState, kiểm tra quyền sở hữu, lưu DB rồi RedirectToAction về trang gọi.

        // Môn học (bảng courses)
        [HttpPost, ValidateAntiForgeryToken] public IActionResult CreateCourse(CreateCourseInput input) => NotReady("thêm môn học");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult UpdateCourse(UpdateCourseInput input) => NotReady("sửa môn học");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult DeleteCourse(int id) => NotReady("xóa môn học");

        // Thời khóa biểu (bảng course_schedules). Back-end cần chặn buổi học trùng giờ cùng ngày.
        [HttpPost, ValidateAntiForgeryToken] public IActionResult CreateScheduleSlot(SaveScheduleSlotInput input) => NotReady("thêm buổi học");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult UpdateScheduleSlot(SaveScheduleSlotInput input) => NotReady("sửa buổi học");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult DeleteScheduleSlot(int id) => NotReady("xóa buổi học");

        // Bài tập và kỳ thi (bảng assignments)
        [HttpPost, ValidateAntiForgeryToken] public IActionResult CreateAssignment(CreateAssignmentInput input) => NotReady("thêm bài tập");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult UpdateAssignment(UpdateAssignmentInput input) => NotReady("sửa bài tập");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult DeleteAssignment(int id) => NotReady("xóa bài tập");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult ToggleSubmitted(int id) => NotReady("đánh dấu đã nộp");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult RecordScore(RecordScoreInput input) => NotReady("nhập điểm");

        // Điểm số
        [HttpPost, ValidateAntiForgeryToken] public IActionResult SetTargetScore(SetTargetScoreInput input) => NotReady("đặt điểm mục tiêu");

        // Tài liệu học tập
        [HttpPost, ValidateAntiForgeryToken] public IActionResult CreateDocument(CreateDocumentInput input) => NotReady("thêm tài liệu");
        [HttpPost, ValidateAntiForgeryToken] public IActionResult DeleteDocument(int id) => NotReady("xóa tài liệu");

        private IActionResult NotReady(string what) =>
            StatusCode(StatusCodes.Status501NotImplemented, $"Chức năng {what} chưa được triển khai.");
    }
}