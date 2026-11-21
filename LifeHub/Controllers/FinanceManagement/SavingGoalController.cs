using LifeHub.DataAccessLayer;
using LifeHub.Models.FinanceManagement;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.FinanceModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class SavingGoalController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SavingGoalController(ApplicationDbContext context) => _context = context;
        // GET /SavingGoal?status=active|achieved|all
        public async Task<IActionResult> Index(string? status)
        {
            ViewData["Title"] = "Mục tiêu tiết kiệm";
            ViewData["ActivePage"] = "SavingGoal";

            var userId=User.GetUserId(); var goals=await _context.SavingsGoals.AsNoTracking().Where(g=>g.UserId==userId).OrderBy(g=>g.Deadline).ToListAsync();
            var wallets=await _context.Wallets.AsNoTracking().Where(w=>w.UserId==userId&&w.IsActive).Select(w=>new WalletOption {Id=w.Id,Name=w.Name,Kind=w.WalletType.ToLowerInvariant(),Balance=w.InitialBalance}).ToListAsync();
            var key=(status??"active").ToLowerInvariant(); if(key is not ("active" or "achieved" or "all")) key="active";
            var cards=goals.Select(g=>new SavingGoalCard {Id=g.Id,Title=g.Title,TargetAmount=g.TargetAmount,CurrentAmount=g.CurrentAmount??0m,Deadline=g.Deadline?.ToDateTime(TimeOnly.MinValue),Color=g.Color??"#e11d48"}).ToList();
            return View("~/Views/Finance/SavingGoal.cshtml",new SavingGoalPageViewModel {Status=key,Goals=cards.Where(g=>key=="all"||(key=="achieved"?g.IsAchieved:!g.IsAchieved)).ToList(),SourceWallets=wallets.Where(w=>w.Kind!="savings").ToList(),TotalSaved=cards.Sum(g=>g.CurrentAmount),TotalTarget=cards.Sum(g=>g.TargetAmount),ActiveCount=cards.Count(g=>!g.IsAchieved),AchievedCount=cards.Count(g=>g.IsAchieved)});
        }

        [HttpGet]
        public IActionResult Create() => View("~/Views/Finance/SavingGoal/Create.cshtml");

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var uid=User.GetUserId();var g=await _context.SavingsGoals.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==uid);if(g==null)return NotFound();
            ViewBag.Id=g.Id;ViewBag.Title=g.Title;ViewBag.TargetAmount=g.TargetAmount;ViewBag.Deadline=g.Deadline?.ToString("yyyy-MM-dd");ViewBag.Color=g.Color;
            return View("~/Views/Finance/SavingGoal/Edit.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var uid=User.GetUserId();var g=await _context.SavingsGoals.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==uid);if(g==null)return NotFound();
            ViewBag.Id=g.Id;ViewBag.Title=g.Title;return View("~/Views/Finance/SavingGoal/Delete.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Contribute(int id)
        {
            var uid = User.GetUserId();
            if (!await LoadContributionViewData(id, uid)) return NotFound();
            return View("~/Views/Finance/SavingGoal/Contribute.cshtml", new ContributeInput { GoalId = id });
        }

        // POST /SavingGoal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveSavingGoalInput input)
        {
            input.Title=input.Title?.Trim()??"";if(!ModelState.IsValid||input.TargetAmount<=0||!ValidColor(input.Color)){ModelState.AddModelError(string.Empty,"Thông tin mục tiêu không hợp lệ.");return View("~/Views/Finance/SavingGoal/Create.cshtml",input);}
            var uid=User.GetUserId();_context.SavingsGoals.Add(new SavingsGoal {UserId=uid,Title=input.Title,TargetAmount=input.TargetAmount,CurrentAmount=0,Deadline=input.Deadline.HasValue?DateOnly.FromDateTime(input.Deadline.Value):null,Color=input.Color,IsAchieved=false,CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã tạo mục tiêu.";return RedirectToAction(nameof(Index));
        }

        // POST /SavingGoal/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaveSavingGoalInput input)
        {
            var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";if(!ModelState.IsValid||input.TargetAmount<=0||!ValidColor(input.Color)){ModelState.AddModelError(string.Empty,"Thông tin mục tiêu không hợp lệ.");ViewBag.Id=input.Id;ViewBag.Title=input.Title;ViewBag.TargetAmount=input.TargetAmount;ViewBag.Deadline=input.Deadline?.ToString("yyyy-MM-dd");ViewBag.Color=input.Color;return View("~/Views/Finance/SavingGoal/Edit.cshtml",input);}
            var g=await _context.SavingsGoals.FirstOrDefaultAsync(x=>x.Id==input.Id&&x.UserId==uid);if(g==null)return NotFound();g.Title=input.Title;g.TargetAmount=input.TargetAmount;g.Deadline=input.Deadline.HasValue?DateOnly.FromDateTime(input.Deadline.Value):null;g.Color=input.Color;g.IsAchieved=(g.CurrentAmount??0)>=g.TargetAmount;await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã cập nhật mục tiêu.";return RedirectToAction(nameof(Index));
        }

        // POST /SavingGoal/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(SaveSavingGoalInput input)
        {
            var uid=User.GetUserId();var g=await _context.SavingsGoals.FirstOrDefaultAsync(x=>x.Id==input.Id&&x.UserId==uid);if(g==null)return NotFound();
            if((g.CurrentAmount??0)>0||await _context.Transactions.AnyAsync(t=>t.UserId==uid&&t.SavingsGoalId==g.Id)){TempData["ErrorMessage"]="Mục tiêu đã có đóng góp nên không thể xóa.";return RedirectToAction(nameof(Index));}
            _context.SavingsGoals.Remove(g);await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã xóa mục tiêu.";return RedirectToAction(nameof(Index));
        }

        // POST /SavingGoal/Contribute
        // Ghi khoản đóng góp, cập nhật số dư ví và tiến độ mục tiêu trong cùng một transaction DB.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contribute(ContributeInput input)
        {
            var uid = User.GetUserId();
            if (!ModelState.IsValid)
            {
                if (!await LoadContributionViewData(input.GoalId, uid)) return NotFound();
                return View("~/Views/Finance/SavingGoal/Contribute.cshtml", input);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var goal = await _context.SavingsGoals.FirstOrDefaultAsync(g => g.Id == input.GoalId && g.UserId == uid && !g.IsAchieved);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == input.WalletId && w.UserId == uid && w.IsActive && w.WalletType.ToLower() != "savings");
            if (goal == null) return NotFound();
            if (wallet == null)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(nameof(input.WalletId), "Hãy chọn một ví nguồn hợp lệ.");
                await LoadContributionViewData(input.GoalId, uid);
                return View("~/Views/Finance/SavingGoal/Contribute.cshtml", input);
            }
            if (wallet.InitialBalance < input.Amount)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(nameof(input.Amount), "Số dư ví nguồn không đủ cho khoản đóng góp này.");
                await LoadContributionViewData(input.GoalId, uid);
                return View("~/Views/Finance/SavingGoal/Contribute.cshtml", input);
            }

            wallet.InitialBalance -= input.Amount;
            goal.CurrentAmount = (goal.CurrentAmount ?? 0) + input.Amount;
            goal.IsAchieved = goal.CurrentAmount >= goal.TargetAmount;
            var note = $"Đóng góp: {goal.Title}";
            _context.Transactions.Add(new Transaction
            {
                UserId = uid,
                WalletId = wallet.Id,
                SavingsGoalId = goal.Id,
                TransactionType = "Expense",
                Amount = input.Amount,
                TransactionDate = DateOnly.FromDateTime(DateTime.Today),
                Note = note.Length > 255 ? $"Đóng góp mục tiêu #{goal.Id}" : note,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["SuccessMessage"] = "Đã ghi nhận đóng góp.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> LoadContributionViewData(int goalId, int userId)
        {
            var goal = await _context.SavingsGoals.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId && !g.IsAchieved);
            if (goal == null) return false;

            ViewBag.GoalTitle = goal.Title;
            ViewBag.CurrentAmount = goal.CurrentAmount ?? 0m;
            ViewBag.TargetAmount = goal.TargetAmount;
            ViewBag.Wallets = await _context.Wallets.AsNoTracking()
                .Where(w => w.UserId == userId && w.IsActive && w.WalletType.ToLower() != "savings")
                .OrderBy(w => w.Name)
                .Select(w => new WalletOption
                {
                    Id = w.Id,
                    Name = w.Name,
                    Kind = w.WalletType.ToLower(),
                    Balance = w.InitialBalance
                })
                .ToListAsync();
            return true;
        }

        private static bool ValidColor(string? color)=>color is {Length:7}&&color[0]=='#'&&color.Skip(1).All(Uri.IsHexDigit);
    }
}
