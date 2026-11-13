using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.FinanceManagement;
using LifeHub.Models.ViewModels.FinanceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // GET: /Finance
        public async Task<IActionResult> Index()
        {
            int userId = User.GetUserId();

            int month = DateTime.Today.Month;
            int year = DateTime.Today.Year;

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1);

            // Lấy giao dịch trong tháng hiện tại của người dùng.
            var monthlyTransactions = await _context.Transactions
                .Where(t => t.UserId == userId
                    && t.TransactionDate >= startDate
                    && t.TransactionDate < endDate)
                .ToListAsync();

            decimal totalIncome = monthlyTransactions
                .Where(t => t.TransactionType.ToLower() == "income")
                .Sum(t => t.Amount);

            decimal totalExpense = monthlyTransactions
                .Where(t => t.TransactionType.ToLower() == "expense")
                .Sum(t => t.Amount);

            // Lấy danh mục mặc định và danh mục do người dùng tạo.
            var categories = await _context.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Lấy ngân sách trong tháng.
            var budgets = await _context.Budgets
                .Where(b => b.UserId == userId
                    && b.Month == month
                    && b.Year == year)
                .ToListAsync();

            // Tính chi tiêu và ngân sách theo từng danh mục.
            var categoryViewModels = categories
                .Select(c =>
                {
                    var isExpense = c.Type.ToLower() == "expense";

                    var spent = isExpense
                        ? monthlyTransactions
                            .Where(t => t.CategoryId == c.Id
                                && t.TransactionType.ToLower() == "expense")
                            .Sum(t => t.Amount)
                        : 0;

                    var limit = isExpense
                        ? budgets
                            .Where(b => b.CategoryId == c.Id)
                            .Sum(b => b.LimitAmount)
                        : 0;

                    return new FinanceCategoryViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = c.Type,
                        Spent = spent,
                        Limit = limit
                    };
                })
                .ToList();

            // Lấy giao dịch gần đây, kèm tên danh mục.
            var recentTransactions = await (
                from t in _context.Transactions
                join c in _context.Categories
                    on t.CategoryId equals c.Id into categoryJoin
                from c in categoryJoin.DefaultIfEmpty()
                where t.UserId == userId
                orderby t.TransactionDate descending, t.CreatedAt descending
                select new FinanceTransactionViewModel
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    Type = t.TransactionType,
                    Name = t.Note ?? "Giao dịch",
                    Category = c != null ? c.Name : "Chưa phân loại",
                    Date = t.TransactionDate.ToDateTime(TimeOnly.MinValue),
                    Amount = t.Amount
                }
            )
            .Take(6)
            .ToListAsync();

            // Lấy ngân sách tháng.
            decimal monthlyBudget = budgets.Sum(b => b.LimitAmount);

            // Lấy thông tin mục tiêu tiết kiệm.
            var savingGoals = await _context.SavingsGoals
                .Where(s => s.UserId == userId)
                .ToListAsync();

            decimal savingGoal = savingGoals.Sum(s => s.TargetAmount);
            decimal savingProgress = savingGoals.Sum(s => s.CurrentAmount ?? 0);

            var model = new FinanceViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                TotalSaving = savingProgress,

                MonthlyBudget = monthlyBudget,
                SavingGoal = savingGoal,
                SavingProgress = savingProgress,

                Categories = categoryViewModels,
                Transactions = recentTransactions
            };

            ViewData["Title"] = "Quản lý tài chính";
            ViewData["ActivePage"] = "Finance";

            return View(model);
        }

        // POST: /Finance/CreateTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(
            string name,
            string type,
            int categoryId,
            decimal amount,
            DateOnly date)
        {
            int userId = User.GetUserId();

            // Kiểm tra dữ liệu đầu vào.
            name = name?.Trim() ?? string.Empty;
            type = type?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) ||
                name.Length > 255 ||
                amount <= 0 ||
                (type != "income" && type != "expense"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Thông tin giao dịch không hợp lệ."
                });
            }

            // Chỉ chấp nhận danh mục mặc định hoặc danh mục của người dùng.
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == categoryId &&
                    (c.UserId == null || c.UserId == userId));

            if (category == null || category.Type.ToLower() != type)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Danh mục không hợp lệ hoặc không phù hợp với loại giao dịch."
                });
            }

            // Tìm ví đang hoạt động của người dùng.
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w =>
                    w.UserId == userId && w.IsActive);

            // Nếu chưa có ví, tự động tạo ví tiền mặt mặc định.
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Name = "Ví tiền mặt",
                    WalletType = "Cash",
                    InitialBalance = 0,
                    Currency = "VND",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Tạo giao dịch mới.
            var transaction = new Transaction
            {
                UserId = userId,
                WalletId = wallet.Id,
                CategoryId = category.Id,
                TransactionType = type,
                Amount = amount,
                TransactionDate = date,
                Note = name,
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã lưu giao dịch thành công."
            });
        }
    }
}