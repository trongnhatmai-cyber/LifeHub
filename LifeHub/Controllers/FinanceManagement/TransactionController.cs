using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.FinanceModules;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class TransactionController : Controller
    {
        // GET /Transaction?Type=expense&CategoryId=1&WalletId=2&Month=2026-11&From=...&To=...&Q=...
        // Lọc thu/chi, danh mục, ví, tháng, khoảng ngày và từ khóa đều xử lý ở đây, tổng thu/chi tính ở C#.
        public IActionResult Index(TransactionFilter filter)
        {
            ViewData["Title"] = "Giao dịch thu chi";
            ViewData["ActivePage"] = "Transaction";

            // ĐIỂM THAY THẾ DUY NHẤT: sau này đổi sang service/DB, View không phải sửa.
            return View("~/Views/Finance/Transaction/Index.cshtml", FinanceModulesMockData.GetTransactionPage(filter));
        }

        // POST /Transaction/Create
        // TODO (Back-end): kiểm tra ModelState, kiểm tra danh mục cùng loại với giao dịch, lưu bảng transactions,
        // cập nhật số dư ví, rồi RedirectToAction(nameof(Index)).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SaveTransactionInput input) => NotReady("thêm giao dịch");

        // POST /Transaction/Edit
        // TODO (Back-end): chỉ chủ giao dịch được sửa; cập nhật lại số dư ví nếu đổi ví hoặc số tiền.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SaveTransactionInput input) => NotReady("sửa giao dịch");

        // POST /Transaction/Delete
        // TODO (Back-end): xóa (hoặc đánh dấu xóa) và hoàn lại số dư ví.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id) => NotReady("xóa giao dịch");

        private IActionResult NotReady(string what) =>
            StatusCode(StatusCodes.Status501NotImplemented, $"Chức năng {what} chưa được triển khai.");
    }
}