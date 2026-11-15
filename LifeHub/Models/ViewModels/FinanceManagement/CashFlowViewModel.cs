using System;
using System.Collections.Generic;

//Dữ liệu biểu đồ dòng tiền
namespace LifeHub.Models.ViewModels.FinanceManagement
{
    public class CashFlowViewModel
    {
        // Dữ liệu biểu đồ theo tháng
        public List<CashFlowPointViewModel> MonthlyData { get; set; } = new();

        // Dữ liệu biểu đồ theo từng tuần
        public List<CashFlowWeekViewModel> WeeklyData { get; set; } = new();
    }

    public class CashFlowWeekViewModel
    {
        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public string Label { get; set; } = string.Empty;

        public List<CashFlowPointViewModel> Days { get; set; } = new();
    }

    public class CashFlowPointViewModel
    {
        public DateTime Date { get; set; }

        public string Label { get; set; } = string.Empty;

        public decimal Income { get; set; }

        public decimal Expense { get; set; }
    }
}