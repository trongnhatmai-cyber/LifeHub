using LifeHub.Models.MockData;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class FinanceReportController : Controller
    {
        // GET /FinanceReport?month=2026-11
        // Tên FinanceReport để không trùng với phân hệ "Thống kê và báo cáo" chung (ReportController nếu có).
        public IActionResult Index(string? month)
        {
            ViewData["Title"] = "Báo cáo tài chính";
            ViewData["ActivePage"] = "FinanceReport";

            // ĐIỂM THAY THẾ DUY NHẤT: sau này đổi sang truy vấn tổng hợp từ DB, View không phải sửa.
            return View(FinanceModulesMockData.GetReport(month));
        }
    }
}