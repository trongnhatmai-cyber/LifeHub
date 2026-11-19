using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.FinanceManagement;
using LifeHub.Models.ViewModels.FinanceModules;
using LifeHub.Models.FinanceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;
        public WalletController(ApplicationDbContext context) => _context = context;
        // GET /Wallet
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Ví và tài khoản";
            ViewData["ActivePage"] = "Wallet";

            var userId=User.GetUserId(); var start=new DateOnly(DateTime.Today.Year,DateTime.Today.Month,1);
            var wallets=await _context.Wallets.AsNoTracking().Where(w=>w.UserId==userId).OrderBy(w=>w.Name).ToListAsync();
            var ids=wallets.Select(w=>w.Id).ToArray();
            var tx=await _context.Transactions.AsNoTracking().Where(t=>t.UserId==userId&&ids.Contains(t.WalletId)&&t.TransactionDate>=start).OrderByDescending(t=>t.TransactionDate).Take(100).ToListAsync();
            var categories=await _context.Categories.AsNoTracking().Where(c=>c.UserId==null||c.UserId==userId).ToDictionaryAsync(c=>c.Id);
            var cards=wallets.Select(w=>new WalletCard {Id=w.Id,Name=w.Name,Kind=w.WalletType.ToLowerInvariant(),Color=w.Color??"#e11d48",Balance=w.InitialBalance,
                MonthIncome=tx.Where(t=>t.WalletId==w.Id&&t.TransactionType=="Income").Sum(t=>t.Amount),MonthExpense=tx.Where(t=>t.WalletId==w.Id&&t.TransactionType=="Expense").Sum(t=>t.Amount),
                Recent=tx.Where(t=>t.WalletId==w.Id).Take(5).Select(t=>new TransactionRow {Id=t.Id,Date=t.TransactionDate.ToDateTime(TimeOnly.MinValue),Name=t.Note??"Giao dịch",Type=t.TransactionType.ToLowerInvariant(),CategoryId=t.CategoryId??0,CategoryName=t.CategoryId.HasValue&&categories.TryGetValue(t.CategoryId.Value,out var c)?c.Name:"Chuyển tiền",CategoryColor=t.CategoryId.HasValue&&categories.TryGetValue(t.CategoryId.Value,out var c2)?c2.Color??"#e98ba5":"#e98ba5",WalletId=w.Id,WalletName=w.Name,Amount=t.Amount}).ToList() }).ToList();
            return View("~/Views/Finance/Wallet/Index.cshtml", new WalletPageViewModel {Wallets=cards});
        }

        [HttpGet]
        public IActionResult Create() => View("~/Views/Finance/Wallet/Create.cshtml");

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId=User.GetUserId(); var wallet=await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w=>w.Id==id&&w.UserId==userId);
            if(wallet==null)return NotFound();
            ViewBag.Id=wallet.Id; ViewBag.Name=wallet.Name; ViewBag.Kind=wallet.WalletType; ViewBag.Color=wallet.Color;
            return View("~/Views/Finance/Wallet/Edit.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId=User.GetUserId(); var wallet=await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w=>w.Id==id&&w.UserId==userId);
            if(wallet==null)return NotFound(); ViewBag.Id=wallet.Id; ViewBag.Name=wallet.Name;
            return View("~/Views/Finance/Wallet/Delete.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Transfer()
        {
            await PopulateWalletOptionsAsync(User.GetUserId());
            return View("~/Views/Finance/Wallet/Transfer.cshtml");
        }

        // POST /Wallet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveWalletInput input)
        {
            input.Name=input.Name?.Trim()??"";
            if(!ModelState.IsValid||!AllowedKind(input.Kind)){ModelState.AddModelError(nameof(input.Kind),"Loại ví không hợp lệ.");return View("~/Views/Finance/Wallet/Create.cshtml",input);}
            var userId=User.GetUserId();
            _context.Wallets.Add(new Wallet {UserId=userId,Name=input.Name,WalletType=input.Kind,Currency="VND",Color=ValidColor(input.Color)?input.Color:"#e11d48",InitialBalance=0m,IsActive=true,CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync(); TempData["SuccessMessage"]="Đã tạo ví."; return RedirectToAction(nameof(Index));
        }

        // POST /Wallet/Edit: chỉ cập nhật thông tin ví, giữ nguyên số dư.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaveWalletInput input)
        {
            var userId=User.GetUserId(); input.Name=input.Name?.Trim()??"";
            if(!ModelState.IsValid||!AllowedKind(input.Kind)||!ValidColor(input.Color)){ModelState.AddModelError(string.Empty,"Thông tin ví không hợp lệ.");ViewBag.Id=input.Id;ViewBag.Name=input.Name;ViewBag.Kind=input.Kind;ViewBag.Color=input.Color;return View("~/Views/Finance/Wallet/Edit.cshtml",input);}
            var wallet=await _context.Wallets.FirstOrDefaultAsync(w=>w.Id==input.Id&&w.UserId==userId); if(wallet==null)return NotFound();
            wallet.Name=input.Name; wallet.WalletType=input.Kind; wallet.Color=input.Color; await _context.SaveChangesAsync();
            TempData["SuccessMessage"]="Đã cập nhật ví."; return RedirectToAction(nameof(Index));
        }

        // POST /Wallet/Delete: chỉ xóa ví chưa có số dư và chưa phát sinh giao dịch.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(SaveWalletInput input)
        {
            var id=input.Id;
            var userId=User.GetUserId(); var wallet=await _context.Wallets.FirstOrDefaultAsync(w=>w.Id==id&&w.UserId==userId); if(wallet==null)return NotFound();
            if(wallet.InitialBalance!=0||await _context.Transactions.AnyAsync(t=>t.UserId==userId&&(t.WalletId==id||t.ToWalletId==id)))
            {TempData["ErrorMessage"]="Chỉ có thể xóa ví chưa có số dư và chưa phát sinh giao dịch.";return RedirectToAction(nameof(Index));}
            _context.Wallets.Remove(wallet); await _context.SaveChangesAsync(); TempData["SuccessMessage"]="Đã xóa ví."; return RedirectToAction(nameof(Index));
        }

        // POST /Wallet/Transfer: chuyển số dư và ghi giao dịch trong cùng một transaction DB.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferInput input)
        {
            var userId=User.GetUserId();
            if(!ModelState.IsValid||input.FromWalletId==input.ToWalletId){ModelState.AddModelError(string.Empty,"Hãy chọn hai ví khác nhau và số tiền hợp lệ.");await PopulateWalletOptionsAsync(userId);return View("~/Views/Finance/Wallet/Transfer.cshtml",input);}
            await using var dbTransaction=await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var wallets=await _context.Wallets.Where(w=>w.UserId==userId&&w.IsActive&&(w.Id==input.FromWalletId||w.Id==input.ToWalletId)).OrderBy(w=>w.Id).ToListAsync();
            var source=wallets.FirstOrDefault(w=>w.Id==input.FromWalletId); var target=wallets.FirstOrDefault(w=>w.Id==input.ToWalletId);
            if(source==null||target==null||source.Currency!=target.Currency||source.InitialBalance<input.Amount)
            {await dbTransaction.RollbackAsync();ModelState.AddModelError(string.Empty,"Không thể chuyển tiền. Hãy kiểm tra quyền sở hữu, tiền tệ và số dư.");await PopulateWalletOptionsAsync(userId);return View("~/Views/Finance/Wallet/Transfer.cshtml",input);}
            source.InitialBalance-=input.Amount; target.InitialBalance+=input.Amount;
            _context.Transactions.Add(new Transaction {UserId=userId,WalletId=source.Id,ToWalletId=target.Id,TransactionType="Transfer",Amount=input.Amount,TransactionDate=DateOnly.FromDateTime(DateTime.Today),Note=input.Note?.Trim(),CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync(); await dbTransaction.CommitAsync(); TempData["SuccessMessage"]="Đã chuyển tiền."; return RedirectToAction(nameof(Index));
        }

        private async Task PopulateWalletOptionsAsync(int userId)=>ViewBag.Wallets=await _context.Wallets.AsNoTracking().Where(w=>w.UserId==userId&&w.IsActive).OrderBy(w=>w.Name).Select(w=>new WalletOption{Id=w.Id,Name=w.Name,Balance=w.InitialBalance}).ToListAsync();
        private static bool AllowedKind(string? kind)=>kind is "cash" or "bank" or "ewallet" or "savings";
        private static bool ValidColor(string? color)=>color is {Length:7}&&color[0]=='#'&&color.Skip(1).All(Uri.IsHexDigit);
    }
}
