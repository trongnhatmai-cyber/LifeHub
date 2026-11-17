using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.FinanceModules;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class WalletController : Controller
    {
        // GET /Wallet
        public IActionResult Index()
        {
            ViewData["Title"] = "Ví và tài khoản";
            ViewData["ActivePage"] = "Wallet";

            // ĐIỂM THAY THẾ DUY NHẤT: sau này đổi sang service/DB, View không phải sửa.
            return View(FinanceModulesMockData.GetWalletPage());
        }

        // POST /Wallet/Create   TODO (Back-end): lưu bảng wallets.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SaveWalletInput input) => NotReady("thêm ví");

        // POST /Wallet/Edit   TODO (Back-end): cập nhật tên, loại, màu. Không sửa trực tiếp số dư nếu ví đã có giao dịch.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SaveWalletInput input) => NotReady("sửa ví");

        // POST /Wallet/Delete   TODO (Back-end): chỉ cho xóa khi ví chưa có giao dịch.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id) => NotReady("xóa ví");

        // POST /Wallet/Transfer   TODO (Back-end): tạo giao dịch chuyển ví, trừ ví nguồn, cộng ví đích trong một transaction DB.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Transfer(TransferInput input) => NotReady("chuyển tiền giữa các ví");

        private IActionResult NotReady(string what) =>
            StatusCode(StatusCodes.Status501NotImplemented, $"Chức năng {what} chưa được triển khai.");
    }
}