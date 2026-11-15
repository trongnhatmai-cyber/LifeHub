using System;
using LifeHub.Models.MockData;
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

            var model = FinanceMockData.GetData();

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
