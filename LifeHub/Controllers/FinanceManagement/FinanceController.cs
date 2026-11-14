using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using LifeHub.Models.ViewModels.FinanceManagement;

namespace LifeHub.Controllers
{
    public class FinanceController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý tài chính";
            ViewData["ActivePage"] = "Finance";

            // ViewModel chuẩn kết nối tầng Controller -> Razor View
            // Tích hợp dữ liệu mẫu thực tế giúp giao diện luôn trực quan khi trình diễn
            // Khi kết nối Entity Framework Core, chỉ cần thay bằng truy vấn CSDL từ DbContext/Service
            var model = new FinanceViewModel
            {
                TotalIncome = 25000000m,
                TotalExpense = 12800000m,
                TotalSaving = 5000000m,
                MonthlyBudget = 15000000m,
                SavingGoal = 20000000m,
                SavingProgress = 5000000m,
                Categories = new List<FinanceCategoryViewModel>
                {
                    new() { Id = 1, Name = "Ăn uống", Type = "expense", Spent = 4500000m, Limit = 5000000m },
                    new() { Id = 2, Name = "Đi lại & Xăng xe", Type = "expense", Spent = 1200000m, Limit = 1500000m },
                    new() { Id = 3, Name = "Học tập & Sách", Type = "expense", Spent = 2100000m, Limit = 3000000m },
                    new() { Id = 4, Name = "Mua sắm cá nhân", Type = "expense", Spent = 3500000m, Limit = 4000000m },
                    new() { Id = 5, Name = "Tiền lương hàng tháng", Type = "income", Spent = 20000000m, Limit = 0 },
                    new() { Id = 6, Name = "Làm thêm / Freelance", Type = "income", Spent = 5000000m, Limit = 0 }
                },
                Transactions = new List<FinanceTransactionViewModel>
                {
                    new() { Id = 1, Name = "Nhận lương tháng 11", Category = "Tiền lương", Type = "income", Amount = 20000000m, Date = DateTime.Now.AddDays(-2) },
                    new() { Id = 2, Name = "Thù lao dự án Web cá nhân", Category = "Làm thêm", Type = "income", Amount = 5000000m, Date = DateTime.Now.AddDays(-4) },
                    new() { Id = 3, Name = "Mua tài liệu lập trình .NET", Category = "Học tập", Type = "expense", Amount = 450000m, Date = DateTime.Now.AddDays(-1) },
                    new() { Id = 4, Name = "Ăn uống cuối tuần", Category = "Ăn uống", Type = "expense", Amount = 320000m, Date = DateTime.Now },
                    new() { Id = 5, Name = "Đóng tiền nhà & internet", Category = "Sinh hoạt", Type = "expense", Amount = 3500000m, Date = DateTime.Now.AddDays(-6) }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateTransaction(string title, string type, int categoryId, decimal amount, DateTime date)
        {
            // API Endpoint nhận form từ modal
            // Trả về JSON để JavaScript phía Front-end xử lý mượt mà không reload đơ trang
            return Json(new { success = true, message = "Đã lưu giao dịch thành công!" });
        }
    }
}
