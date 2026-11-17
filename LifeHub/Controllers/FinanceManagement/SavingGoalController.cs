using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.FinanceModules;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class SavingGoalController : Controller
    {
        // GET /SavingGoal?status=active|achieved|all
        public IActionResult Index(string? status)
        {
            ViewData["Title"] = "Mục tiêu tiết kiệm";
            ViewData["ActivePage"] = "SavingGoal";

            // ĐIỂM THAY THẾ DUY NHẤT: sau này đổi sang service/DB, View không phải sửa.
            return View("~/Views/Finance/SavingGoal/Index.cshtml", FinanceModulesMockData.GetSavingGoalPage(status));
        }

        // POST /SavingGoal/Create   TODO (Back-end): lưu bảng savings_goals.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SaveSavingGoalInput input) => NotReady("thêm mục tiêu tiết kiệm");

        // POST /SavingGoal/Edit   TODO (Back-end): cập nhật tên, số tiền mục tiêu, hạn, màu.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SaveSavingGoalInput input) => NotReady("sửa mục tiêu tiết kiệm");

        // POST /SavingGoal/Delete   TODO (Back-end): hoàn tiền về ví hoặc yêu cầu người dùng chọn ví nhận lại.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id) => NotReady("xóa mục tiêu tiết kiệm");

        // POST /SavingGoal/Contribute
        // TODO (Back-end): tạo giao dịch chuyển ví (ví nguồn → quỹ tiết kiệm) gắn savings_goal_id, tăng current_amount;
        // khi đủ target_amount thì is_achieved = 1 và trao huy hiệu (user_achievements).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contribute(ContributeInput input) => NotReady("nạp tiền vào mục tiêu");

        private IActionResult NotReady(string what) =>
            StatusCode(StatusCodes.Status501NotImplemented, $"Chức năng {what} chưa được triển khai.");
    }
}