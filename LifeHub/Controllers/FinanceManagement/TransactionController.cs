
using LifeHub.BusinessLogicLayer.FinanceManagement;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.FinanceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers.FinanceManagement;

[Authorize]
public class TransactionController : Controller
{
    private readonly TransactionService _transactionService;

    public TransactionController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }
    
    public IActionResult Index()
    {
        return View("~/Views/Finance/Transaction/Index.cshtml");
    }

    // GET: /Transaction/Create
    [HttpGet]
    public IActionResult Create()
    {
        var model = new TransactionCreateViewModel();

        return View(model);
    }

    // POST: /Transaction/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TransactionCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int userId = User.GetUserId();

        var transaction = await _transactionService.CreateAsync(
            userId,
            model.TransactionType,
            model.WalletId,
            model.ToWalletId,
            model.CategoryId,
            model.SavingsGoalId,
            model.Amount,
            model.TransactionDate,
            model.Note
        );

        if (transaction == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể tạo giao dịch. Vui lòng kiểm tra ví, số dư và dữ liệu đã nhập."
            );

            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm giao dịch thành công!";

        return RedirectToAction(
            "Index",
            "Finance"
        );
    }
}
