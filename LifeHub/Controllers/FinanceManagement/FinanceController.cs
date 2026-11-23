using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.FinanceManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace LifeHub.Controllers
{
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = User.GetUserId();

            int currentMonth = DateTime.Today.Month;
            int currentYear = DateTime.Today.Year;

            // Lấy giao dịch trong tháng hiện tại
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId &&
                    t.TransactionDate.Month == currentMonth &&
                    t.TransactionDate.Year == currentYear
                )
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            // Lấy danh mục mặc định và danh mục của người dùng
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.UserId == null || c.UserId == userId)
                .ToListAsync();

            // Lấy ngân sách tháng hiện tại
            var budgets = await _context.Budgets
                .AsNoTracking()
                .Where(b =>
                    b.UserId == userId &&
                    b.Month == currentMonth &&
                    b.Year == currentYear
                )
                .ToListAsync();

            // Lấy mục tiêu tiết kiệm
            var savingGoals = await _context.SavingsGoals
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .ToListAsync();

            // Tính tổng thu nhập và chi tiêu
            decimal totalIncome = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);

            decimal totalExpense = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);

            // Tổng tiền mục tiêu tiết kiệm đã tích lũy
            decimal totalSaving = savingGoals.Sum(g => g.CurrentAmount ?? 0m);

            // Tổng ngân sách tháng
            decimal monthlyBudget = budgets
                .Sum(b => b.LimitAmount);

            // Tổng mục tiêu tiết kiệm
            decimal savingGoal = savingGoals
                .Where(g => !g.IsAchieved)
                .Sum(g => g.TargetAmount);

            // Tính tiến độ tiết kiệm
            decimal totalTarget = savingGoals.Sum(g => g.TargetAmount);

            decimal savingProgress = totalTarget > 0
                ? Math.Round(totalSaving / totalTarget * 100, 2)
                : 0;

            var categoryItems = categories.Select(c =>
            {
                var categoryTransactions = transactions
                    .Where(t =>
                        t.CategoryId == c.Id &&
                        t.TransactionType == "Expense"
                    );

                var budget = budgets
                    .FirstOrDefault(b => b.CategoryId == c.Id);

                return new FinanceCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    Spent = categoryTransactions.Sum(t => t.Amount),
                    Limit = budget?.LimitAmount ?? 0
                };
            }).ToList();

            var transactionItems = transactions.Select(t =>
            {
                var category = categories
                    .FirstOrDefault(c => c.Id == t.CategoryId);

                return new FinanceTransactionViewModel
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    Type = t.TransactionType,
                    Name = t.Note ?? category?.Name ?? "Giao dịch",
                    Category = category?.Name ?? "Chưa phân loại",
                    Date = t.TransactionDate.ToDateTime(TimeOnly.MinValue),
                    Amount = t.Amount
                };
            }).ToList();

            var model = new FinanceViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                TotalSaving = totalSaving,

                MonthlyBudget = monthlyBudget,
                SavingGoal = savingGoal,
                SavingProgress = savingProgress,

                Categories = categoryItems,
                Transactions = transactionItems
            };

            ViewBag.Wallets = await _context.Wallets.AsNoTracking()
                .Where(w => w.UserId == userId && w.IsActive).OrderBy(w => w.Name)
                .Select(w => new { w.Id, w.Name, Balance = w.InitialBalance }).ToListAsync();
            ViewBag.SavingsGoals = await _context.SavingsGoals.AsNoTracking()
                .Where(g => g.UserId == userId && !g.IsAchieved).OrderBy(g => g.Title)
                .Select(g => new { g.Id, Name = g.Title }).ToListAsync();

            return View(model);
        }
    }
}
