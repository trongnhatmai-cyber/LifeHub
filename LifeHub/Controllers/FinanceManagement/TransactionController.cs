
using LifeHub.BusinessLogicLayer.FinanceManagement;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.FinanceManagement;
using LifeHub.Models.ViewModels.FinanceManagement;
using LifeHub.Models.ViewModels.FinanceModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers.FinanceManagement;

[Authorize]
public class TransactionController : Controller
{
    private readonly TransactionService _transactionService;
    private readonly ApplicationDbContext _context;

    public TransactionController(TransactionService transactionService, ApplicationDbContext context)
    {
        _transactionService = transactionService;
        _context = context;
    }

[HttpGet]
    public async Task<IActionResult> Index(
        string? Month,
        string? Type,
        int? CategoryId,
        int? WalletId,
        DateOnly? From,
        DateOnly? To,
        string? Q)
    {
        int userId = User.GetUserId();

        // ==========================================
        // 1. QUERY GIAO DỊCH CỦA NGƯỜI DÙNG
        // ==========================================

        var userTransactions = _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        // ==========================================
        // 2. XÁC ĐỊNH THÁNG ĐANG XEM
        // ==========================================

        string effectiveMonth = "all";

        if (!string.IsNullOrWhiteSpace(Month))
        {
            var parts = Month.Split('-');

            if (parts.Length == 2
                && int.TryParse(parts[0], out int year)
                && int.TryParse(parts[1], out int month)
                && year >= 1
                && month >= 1
                && month <= 12)
            {
                effectiveMonth = $"{year:D4}-{month:D2}";
            }
        }

        var periodQuery = userTransactions;

        if (effectiveMonth != "all")
        {
            var parts = effectiveMonth.Split('-');
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1);

            periodQuery = periodQuery.Where(t =>
                t.TransactionDate >= startDate &&
                t.TransactionDate < endDate);
        }

        // ==========================================
        // 3. THỐNG KÊ THU CHI
        // ==========================================

        decimal totalIncome = await periodQuery
            .Where(t => t.TransactionType.ToLower() == "income")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        decimal totalExpense = await periodQuery
            .Where(t => t.TransactionType.ToLower() == "expense")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        // ==========================================
        // 4. TẠO DANH SÁCH THÁNG
        // ==========================================

        var monthData = await userTransactions
            .Select(t => new
            {
                Year = t.TransactionDate.Year,
                Month = t.TransactionDate.Month
            })
            .Distinct()
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync();

        var months = monthData.Select(x =>
            new TransactionMonthViewModel
            {
                Key = $"{x.Year:D4}-{x.Month:D2}",
                Label = $"{x.Month:D2}/{x.Year}"
            }).ToList();

        // ==========================================
        // 5. LẤY DANH MỤC
        // ==========================================

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new TransactionCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        var categoryData = await _context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null || c.UserId == userId)
            .ToDictionaryAsync(c => c.Id);

        // ==========================================
        // 6. LẤY DANH SÁCH VÍ
        // ==========================================

        var wallets = await _context.Wallets
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => new TransactionWalletViewModel
            {
                Id = w.Id,
                Name = w.Name
            })
            .ToListAsync();

        var walletData = await _context.Wallets
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToDictionaryAsync(w => w.Id);

        // ==========================================
        // 7. ÁP DỤNG BỘ LỌC
        // ==========================================

        var query = periodQuery;

        string filterType = Type?.ToLowerInvariant() ?? "all";

        if (filterType == "income")
        {
            query = query.Where(t =>
                t.TransactionType.ToLower() == "income");
        }
        else if (filterType == "expense")
        {
            query = query.Where(t =>
                t.TransactionType.ToLower() == "expense");
        }
        else
        {
            filterType = "all";
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == CategoryId.Value);
        }

        if (WalletId.HasValue)
        {
            query = query.Where(t =>
                t.WalletId == WalletId.Value ||
                t.ToWalletId == WalletId.Value);
        }

        if (From.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate >= From.Value);
        }

        if (To.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate <= To.Value);
        }

        if (!string.IsNullOrWhiteSpace(Q))
        {
            string keyword = Q.Trim();

            query = query.Where(t =>
                t.Note != null &&
                t.Note.Contains(keyword));
        }

        // ==========================================
        // 8. LẤY DANH SÁCH GIAO DỊCH
        // ==========================================

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        var rows = transactions.Select(t =>
        {
            categoryData.TryGetValue(
                t.CategoryId ?? 0,
                out var category);

            walletData.TryGetValue(
                t.WalletId,
                out var wallet);

            bool isIncome =
                t.TransactionType.Equals(
                    "Income",
                    StringComparison.OrdinalIgnoreCase);

            bool isTransfer =
                t.TransactionType.Equals(
                    "Transfer",
                    StringComparison.OrdinalIgnoreCase);

            string transactionName =
                !string.IsNullOrWhiteSpace(t.Note)
                    ? t.Note
                    : isIncome
                        ? "Thu nhập"
                        : isTransfer
                            ? "Chuyển tiền"
                            : "Chi tiêu";

            string amountPrefix = isIncome
                ? "+"
                : isTransfer
                    ? "↔ "
                    : "−";

            return new TransactionRowViewModel
            {
                Id = t.Id,
                Name = transactionName,

                CategoryName = category?.Name
                    ?? (isTransfer ? "Chuyển tiền" : "Chưa phân loại"),

                CategoryColor = category?.Color,

                WalletName = wallet?.Name ?? "Không xác định",

                Date = t.TransactionDate,

                Amount = t.Amount,

                IsIncome = isIncome,

                AmountLabel =
                    $"{amountPrefix}{t.Amount:N0} ₫"
            };
        }).ToList();

        // ==========================================
        // 9. TẠO VIEWMODEL
        // ==========================================

        string periodLabel = "Tất cả thời gian";

        if (effectiveMonth != "all")
        {
            var parts = effectiveMonth.Split('-');
            periodLabel = $"{parts[1]}/{parts[0]}";
        }

        var model = new TransactionViewModel
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,

            EffectiveMonth = effectiveMonth,
            PeriodLabel = periodLabel,

            Filter = new TransactionFilterViewModel
            {
                Type = filterType,
                CategoryId = CategoryId,
                WalletId = WalletId,
                From = From,
                To = To,
                Q = Q
            },

            Months = months,
            Categories = categories,
            Wallets = wallets,
            Rows = rows
        };

        return View("~/Views/Finance/Transaction/Index.cshtml", model);
    }

    // GET: /Transaction/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TransactionCreateViewModel();
        await PopulateCreateOptionsAsync(User.GetUserId());
        return View("~/Views/Finance/Transaction/Create.cshtml", model);
    }

    // POST: /Transaction/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Microsoft.AspNetCore.Http.IFormCollection form)
    {
        int userId = User.GetUserId();
        var model = new TransactionCreateViewModel();
        var prefix = Request.Form.Keys.Any(k => k.StartsWith("CreateTransaction.", StringComparison.Ordinal));
        await TryUpdateModelAsync(model, prefix ? "CreateTransaction" : string.Empty);
        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(userId);
            return View("~/Views/Finance/Transaction/Create.cshtml", model);
        }
        var transaction = await _transactionService.CreateAsync(userId, model.TransactionType, model.WalletId,
            model.ToWalletId, model.CategoryId, model.SavingsGoalId, model.Amount, model.TransactionDate, model.Note);
        if (transaction == null)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu giao dịch. Hãy kiểm tra ví, danh mục, số dư và dữ liệu.");
            await PopulateCreateOptionsAsync(userId);
            return View("~/Views/Finance/Transaction/Create.cshtml", model);
        }
        TempData["SuccessMessage"] = "Đã lưu giao dịch.";
        return RedirectToAction("Index", "Finance");
    }

    private async Task PopulateCreateOptionsAsync(int userId)
    {
        ViewBag.Wallets = await _context.Wallets.AsNoTracking().Where(w => w.UserId == userId && w.IsActive)
            .OrderBy(w => w.Name).Select(w => new { w.Id, w.Name }).ToListAsync();
        ViewBag.Categories = await _context.Categories.AsNoTracking().Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.Type }).ToListAsync();
        ViewBag.SavingsGoals = await _context.SavingsGoals.AsNoTracking().Where(g => g.UserId == userId && !g.IsAchieved)
            .OrderBy(g => g.Title).Select(g => new { g.Id, Name = g.Title }).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId=User.GetUserId();var tx=await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t=>t.Id==id&&t.UserId==userId);
        if(tx==null||tx.TransactionType=="Transfer")return NotFound();
        await PopulateCreateOptionsAsync(userId);FillEditViewBag(tx);
        return View("~/Views/Finance/Transaction/Edit.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SaveTransactionInput input)
    {
        var userId=User.GetUserId();
        if(!ModelState.IsValid||input.Amount<=0||input.Type is not ("income" or "expense" or "Income" or "Expense"))
        {await PopulateCreateOptionsAsync(userId);FillEditViewBag(input);return View("~/Views/Finance/Transaction/Edit.cshtml");}
        input.Type=input.Type.Equals("income",StringComparison.OrdinalIgnoreCase)?"Income":"Expense";
        if(!await _transactionService.UpdateAsync(userId,input))
        {ModelState.AddModelError(string.Empty,"Không thể cập nhật giao dịch. Hãy kiểm tra quyền sở hữu, danh mục và số dư.");await PopulateCreateOptionsAsync(userId);FillEditViewBag(input);return View("~/Views/Finance/Transaction/Edit.cshtml");}
        TempData["SuccessMessage"]="Đã cập nhật giao dịch.";return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId=User.GetUserId();var tx=await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t=>t.Id==id&&t.UserId==userId);if(tx==null)return NotFound();
        ViewBag.Id=tx.Id;ViewBag.Name=tx.Note??"Giao dịch";return View("~/Views/Finance/Transaction/Delete.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(SaveTransactionInput input)
    {
        var userId=User.GetUserId();if(!await _transactionService.DeleteAsync(userId,input.Id))return NotFound();
        TempData["SuccessMessage"]="Đã xóa giao dịch và cân đối lại số dư.";return RedirectToAction(nameof(Index));
    }

    private void FillEditViewBag(Transaction tx)
    {ViewBag.Id=tx.Id;ViewBag.Name=tx.Note??"";ViewBag.Type=tx.TransactionType.ToLowerInvariant();ViewBag.CategoryId=tx.CategoryId;ViewBag.WalletId=tx.WalletId;ViewBag.Amount=tx.Amount;ViewBag.Date=tx.TransactionDate.ToString("yyyy-MM-dd");}
    private void FillEditViewBag(SaveTransactionInput input)
    {ViewBag.Id=input.Id;ViewBag.Name=input.Name;ViewBag.Type=input.Type;ViewBag.CategoryId=input.CategoryId;ViewBag.WalletId=input.WalletId;ViewBag.Amount=input.Amount;ViewBag.Date=input.Date.ToString("yyyy-MM-dd");}
}

