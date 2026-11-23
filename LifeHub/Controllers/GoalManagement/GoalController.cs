using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.GoalManagement;
using LifeHub.Models.ViewModels.GoalManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KeyResultEntity = LifeHub.Models.GoalManagement.KeyResult;

namespace LifeHub.Controllers
{
    /// <summary>Phân hệ Mục tiêu: đọc và ghi dữ liệu cá nhân từ SQL qua ApplicationDbContext.</summary>
    [Authorize]
    public class GoalController : Controller
    {
        private readonly ApplicationDbContext _context;
        public GoalController(ApplicationDbContext context) => _context = context;

        private void Page(string title, string activePage)
        {
            ViewData["Title"] = title;
            // Phải trùng khóa mục con trong _Layout.cshtml để mục đó được tô sáng.
            ViewData["ActivePage"] = activePage;
        }

        // ------------------------------------------------------------------ GET

        // GET /Goal
        public async Task<IActionResult> Index()
        {
            Page("Tổng quan mục tiêu", "GoalOverview");
            var (goals, krs) = await Load();
            var rows = krs.Select(ToRow).ToList();
            var activeGoals = goals.Where(g => g.Status == GoalStatusKeys.Active).ToList();

            var model = new GoalOverviewViewModel
            {
                Total = goals.Count,
                ActiveCount = activeGoals.Count,
                CompletedCount = goals.Count(g => g.Status == GoalStatusKeys.Completed),
                KeyResultCount = rows.Count,
                AverageProgress = goals.Count == 0 ? 0 : (int)Math.Round(goals.Average(g => g.Progress)),
                Attention = goals.Where(g => g.Status != GoalStatusKeys.Completed && g.TargetDate.HasValue)
                    .OrderBy(g => g.TargetDate).Take(4).ToList(),
                // Goal chưa có UpdatedAt nên "mới" = mới tạo (CreatedAt), không phải mới cập nhật.
                Recent = goals.OrderByDescending(g => g.Id).Take(4).ToList(),
                TopKeyResults = rows.Where(r => !r.IsCompleted && goals.Any(g => g.Id == r.GoalId && g.Status == GoalStatusKeys.Active))
                    .OrderByDescending(r => r.Percent).Take(5).ToList(),
                ByCategory = goals.GroupBy(g => g.Category).OrderByDescending(x => x.Count()).ToDictionary(x => x.Key, x => x.Count())
            };
            return View(model);
        }

        // GET /Goal/GoalList?status=active|paused|completed&category=study&type=short|long&sort=due|progress|newest&q=...
        public async Task<IActionResult> GoalList(string? status, string? category, string? type, string? sort, string? q)
        {
            Page("Danh sách mục tiêu", "GoalList");
            var (goals, _) = await Load();
            var st = (status ?? "all").ToLowerInvariant();
            var ty = (type ?? "all").ToLowerInvariant();
            var so = (sort ?? "due").ToLowerInvariant();
            var cat = category ?? "all";

            IEnumerable<GoalCardItem> query = goals;
            if (st is "active" or "paused" or "completed") query = query.Where(g => g.Status == st); else st = "all";
            if (ty is "short" or "long") query = query.Where(g => g.GoalType == ty); else ty = "all";
            if (cat != "all") query = query.Where(g => g.Category == CategoryKey(cat)); 
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || g.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            query = so switch
            {
                "progress" => query.OrderByDescending(g => g.Progress),
                "newest" => query.OrderByDescending(g => g.Id),
                _ => query.OrderBy(g => g.TargetDate.HasValue ? 0 : 1).ThenBy(g => g.TargetDate)
            };

            return View(new GoalListViewModel
            {
                Status = st, Type = ty, Sort = so, Category = cat, Q = q,
                TotalCount = goals.Count,
                ActiveCount = goals.Count(g => g.Status == GoalStatusKeys.Active),
                PausedCount = goals.Count(g => g.Status == GoalStatusKeys.Paused),
                CompletedCount = goals.Count(g => g.Status == GoalStatusKeys.Completed),
                Categories = goals.Select(g => g.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList(),
                Items = query.ToList()
            });
        }

        // GET /Goal/KeyResult?goalId=1
        public async Task<IActionResult> KeyResult(int? goalId)
        {
            Page("Kết quả then chốt", "KeyResult");
            var (goals, krs) = await Load();
            var rows = krs.Select(ToRow).ToList();
            if (goalId.HasValue && !goals.Any(g => g.Id == goalId)) goalId = null;

            var groups = goals
                .Where(g => !goalId.HasValue || g.Id == goalId)
                .Select(g => new GoalKeyResultGroup { Goal = g, Items = rows.Where(r => r.GoalId == g.Id).ToList() })
                .Where(x => x.Items.Count > 0 || goalId.HasValue)
                .ToList();

            return View(new GoalKeyResultViewModel
            {
                Total = rows.Count,
                DoneCount = rows.Count(r => r.IsCompleted),
                GoalId = goalId,
                Goals = goals,
                Groups = groups
            });
        }

        // GET /Goal/GoalProgress
        public async Task<IActionResult> GoalProgress()
        {
            Page("Tiến độ mục tiêu", "GoalProgress");
            var (goals, _) = await Load();
            var open = goals.Where(g => g.Status != GoalStatusKeys.Completed).ToList();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // "Chưa đạt kỳ vọng": đã đi quá nửa thời gian từ StartDate đến TargetDate nhưng tiến độ thấp hơn thời gian đã trôi.
            bool Behind(GoalCardItem g)
            {
                if (g.IsOverdue) return true;
                if (!g.StartDate.HasValue || !g.TargetDate.HasValue) return g.Progress < 30 && g.DaysLeft is <= 30;
                var span = g.TargetDate.Value.DayNumber - g.StartDate.Value.DayNumber;
                if (span <= 0) return false;
                var elapsed = Math.Clamp((today.DayNumber - g.StartDate.Value.DayNumber) * 100 / span, 0, 100);
                return g.Progress + 15 < elapsed;
            }

            return View(new GoalProgressViewModel
            {
                Total = goals.Count,
                Average = goals.Count == 0 ? 0 : (int)Math.Round(goals.Average(g => g.Progress)),
                LowCount = goals.Count(g => g.Band == "low"),
                MidCount = goals.Count(g => g.Band == "mid"),
                HighCount = goals.Count(g => g.Band == "high"),
                DoneCount = goals.Count(g => g.Band == "done"),
                Leading = open.Where(g => g.Progress >= 50 && !Behind(g)).OrderByDescending(g => g.Progress).Take(5).ToList(),
                Behind = open.Where(Behind).OrderBy(g => g.Progress).Take(5).ToList(),
                DueSoon = open.Where(g => g.DaysLeft is >= 0 and <= 30).OrderBy(g => g.TargetDate).Take(5).ToList()
            });
        }

        // GET /Goal/GoalReview?id=1
        public async Task<IActionResult> GoalReview(int? id)
        {
            Page("Đánh giá mục tiêu", "GoalReview");
            var (goals, krs) = await Load();
            var selected = goals.FirstOrDefault(g => g.Id == id)
                ?? goals.FirstOrDefault(g => g.Status == GoalStatusKeys.Completed)
                ?? goals.FirstOrDefault();

            return View(new GoalReviewViewModel
            {
                Goals = goals.OrderByDescending(g => g.Status == GoalStatusKeys.Completed).ThenBy(g => g.TargetDate).ToList(),
                Selected = selected,
                KeyResults = selected == null ? new() : krs.Where(k => k.GoalId == selected.Id).Select(ToRow).ToList(),
                ReviewedCount = goals.Count(g => !string.IsNullOrWhiteSpace(g.ReviewNote))
            });
        }

        // ------------------------------------------------------------------ POST

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoal(SaveGoalInput input)
        {
            var userId = User.GetUserId();
            NormalizeGoalInput(input);
            if (!ValidGoalInput(input))
            {
                TempData["ErrorMessage"] = "Thông tin mục tiêu không hợp lệ.";
                return RedirectToAction(nameof(GoalList));
            }

            var goal = new Goal
            {
                UserId = userId,
                Title = input.Title,
                Description = input.Description,
                Category = input.Category,
                GoalType = input.GoalType,
                StartDate = input.StartDate,
                TargetDate = input.TargetDate,
                Status = input.Status,
                ProgressPercent = input.Status == GoalStatusKeys.Completed ? 100 : input.ProgressPercent,
                IsCompleted = input.Status == GoalStatusKeys.Completed,
                CreatedAt = DateTime.Now
            };
            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tạo mục tiêu.";
            return RedirectToAction(nameof(GoalList));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGoal(SaveGoalInput input)
        {
            var userId = User.GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == input.Id && g.UserId == userId);
            if (goal == null) return NotFound();
            NormalizeGoalInput(input);
            if (!ValidGoalInput(input))
            {
                TempData["ErrorMessage"] = "Thông tin mục tiêu không hợp lệ.";
                return RedirectToAction(nameof(GoalList));
            }

            goal.Title = input.Title;
            goal.Description = input.Description;
            goal.Category = input.Category;
            goal.GoalType = input.GoalType;
            goal.StartDate = input.StartDate;
            goal.TargetDate = input.TargetDate;
            goal.Status = input.Status;
            goal.IsCompleted = input.Status == GoalStatusKeys.Completed;
            var hasKeyResults = await _context.KeyResults.AnyAsync(k => k.GoalId == goal.Id);
            goal.ProgressPercent = goal.IsCompleted ? 100 : hasKeyResults ? await CalculateProgressAsync(goal.Id) : input.ProgressPercent;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật mục tiêu.";
            return RedirectToAction(nameof(GoalList));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = User.GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();

            var tasks = await _context.Tasks.Where(t => t.UserId == userId && t.GoalId == id).ToListAsync();
            foreach (var task in tasks) task.GoalId = null;
            var habits = await _context.Habits.Where(h => h.UserId == userId && h.GoalId == id).ToListAsync();
            foreach (var habit in habits) habit.GoalId = null;
            _context.KeyResults.RemoveRange(_context.KeyResults.Where(k => k.GoalId == id));
            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa mục tiêu.";
            return RedirectToAction(nameof(GoalList));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateKeyResult(SaveKeyResultInput input)
        {
            var userId = User.GetUserId();
            input.Title = input.Title?.Trim() ?? string.Empty;
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == input.GoalId && g.UserId == userId);
            if (!ModelState.IsValid || goal == null || string.IsNullOrWhiteSpace(input.Title) || input.CurrentValue > input.TargetValue)
            {
                TempData["ErrorMessage"] = "Thông tin Key Result không hợp lệ.";
                return RedirectToAction(nameof(KeyResult), new { goalId = input.GoalId });
            }

            _context.KeyResults.Add(new KeyResultEntity
            {
                GoalId = goal.Id, Title = input.Title, TargetValue = input.TargetValue,
                CurrentValue = input.CurrentValue, Unit = input.Unit?.Trim(), IsCompleted = input.CurrentValue >= input.TargetValue
            });
            await _context.SaveChangesAsync();
            await RefreshGoalProgressAsync(goal);
            TempData["SuccessMessage"] = "Đã thêm Key Result.";
            return RedirectToAction(nameof(KeyResult), new { goalId = goal.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateKeyResult(SaveKeyResultInput input)
        {
            var userId = User.GetUserId();
            input.Title = input.Title?.Trim() ?? string.Empty;
            var item = await _context.KeyResults.FirstOrDefaultAsync(k => k.Id == input.Id && _context.Goals.Any(g => g.Id == k.GoalId && g.UserId == userId));
            if (item == null) return NotFound();
            var oldGoalId = item.GoalId;
            var newGoal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == input.GoalId && g.UserId == userId);
            if (!ModelState.IsValid || newGoal == null || string.IsNullOrWhiteSpace(input.Title) || input.CurrentValue > input.TargetValue)
            {
                TempData["ErrorMessage"] = "Thông tin Key Result không hợp lệ.";
                return RedirectToAction(nameof(KeyResult), new { goalId = oldGoalId });
            }

            item.GoalId = newGoal.Id;
            item.Title = input.Title;
            item.TargetValue = input.TargetValue;
            item.CurrentValue = input.CurrentValue;
            item.Unit = input.Unit?.Trim();
            item.IsCompleted = input.CurrentValue >= input.TargetValue;
            await _context.SaveChangesAsync();
            var oldGoal = oldGoalId == newGoal.Id ? newGoal : await _context.Goals.FirstAsync(g => g.Id == oldGoalId && g.UserId == userId);
            await RefreshGoalProgressAsync(oldGoal);
            if (oldGoal.Id != newGoal.Id) await RefreshGoalProgressAsync(newGoal);
            TempData["SuccessMessage"] = "Đã cập nhật Key Result.";
            return RedirectToAction(nameof(KeyResult), new { goalId = newGoal.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKeyResult(int id)
        {
            var userId = User.GetUserId();
            var item = await _context.KeyResults.FirstOrDefaultAsync(k => k.Id == id && _context.Goals.Any(g => g.Id == k.GoalId && g.UserId == userId));
            if (item == null) return NotFound();
            var goal = await _context.Goals.FirstAsync(g => g.Id == item.GoalId && g.UserId == userId);
            _context.KeyResults.Remove(item);
            await _context.SaveChangesAsync();
            await RefreshGoalProgressAsync(goal);
            TempData["SuccessMessage"] = "Đã xóa Key Result.";
            return RedirectToAction(nameof(KeyResult), new { goalId = goal.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateKeyResultValue(UpdateKeyResultValueInput input)
        {
            var userId = User.GetUserId();
            var item = await _context.KeyResults.FirstOrDefaultAsync(k => k.Id == input.Id && _context.Goals.Any(g => g.Id == k.GoalId && g.UserId == userId));
            if (item == null) return NotFound();
            if (!ModelState.IsValid || input.CurrentValue > item.TargetValue)
            {
                TempData["ErrorMessage"] = "Giá trị hiện tại phải nằm trong khoảng từ 0 đến giá trị mục tiêu.";
                return RedirectToAction(nameof(KeyResult), new { goalId = item.GoalId });
            }
            item.CurrentValue = input.CurrentValue;
            item.IsCompleted = input.CurrentValue >= item.TargetValue;
            var goal = await _context.Goals.FirstAsync(g => g.Id == item.GoalId && g.UserId == userId);
            await _context.SaveChangesAsync();
            await RefreshGoalProgressAsync(goal);
            TempData["SuccessMessage"] = "Đã cập nhật tiến độ Key Result.";
            return RedirectToAction(nameof(KeyResult), new { goalId = goal.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetGoalStatus(int id, string status, string? returnTo)
        {
            var userId = User.GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();
            if (status is not (GoalStatusKeys.Active or GoalStatusKeys.Paused or GoalStatusKeys.Completed)) return BadRequest();
            goal.Status = status;
            goal.IsCompleted = status == GoalStatusKeys.Completed;
            if (goal.IsCompleted) goal.ProgressPercent = 100;
            else if (await _context.KeyResults.AnyAsync(k => k.GoalId == id)) goal.ProgressPercent = await CalculateProgressAsync(id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái mục tiêu.";
            return returnTo == nameof(GoalReview)
                ? RedirectToAction(nameof(GoalReview), new { id })
                : RedirectToAction(nameof(GoalList));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReview(SaveReviewInput input)
        {
            var userId = User.GetUserId();
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == input.Id && g.UserId == userId);
            if (goal == null) return NotFound();
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Ghi chú đánh giá vượt quá độ dài cho phép.";
                return RedirectToAction(nameof(GoalReview), new { id = input.Id });
            }
            goal.ReviewNote = input.ReviewNote?.Trim();
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu ghi chú đánh giá.";
            return RedirectToAction(nameof(GoalReview), new { id = input.Id });
        }

        // ------------------------------------------------------------------ Helpers

        private static void NormalizeGoalInput(SaveGoalInput input)
        {
            input.Title = input.Title?.Trim() ?? string.Empty;
            input.Description = input.Description?.Trim();
            input.Category = CategoryKey(input.Category);
            input.GoalType = input.GoalType?.Trim().ToLowerInvariant() ?? string.Empty;
            input.Status = input.Status?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static bool ValidGoalInput(SaveGoalInput input) =>
            !string.IsNullOrWhiteSpace(input.Title) && input.Title.Length <= 200 &&
            (string.IsNullOrEmpty(input.Description) || input.Description.Length <= 2000) &&
            GoalCategories.Labels.ContainsKey(input.Category) &&
            input.GoalType is "short" or "long" &&
            input.Status is GoalStatusKeys.Active or GoalStatusKeys.Paused or GoalStatusKeys.Completed &&
            (!input.StartDate.HasValue || !input.TargetDate.HasValue || input.TargetDate.Value >= input.StartDate.Value);

        private async Task RefreshGoalProgressAsync(Goal goal)
        {
            if (goal.IsCompleted)
            {
                goal.ProgressPercent = 100;
                return;
            }
            var results = await _context.KeyResults.Where(k => k.GoalId == goal.Id).ToListAsync();
            goal.ProgressPercent = results.Count == 0 ? 0 : (int)Math.Round(results.Average(k => k.TargetValue <= 0 ? 0 : Math.Clamp((double)((k.CurrentValue ?? 0) / k.TargetValue * 100m), 0, 100)));
            if (results.Count > 0 && results.All(k => k.IsCompleted))
            {
                goal.ProgressPercent = 100;
                goal.IsCompleted = true;
                goal.Status = GoalStatusKeys.Completed;
            }
            await _context.SaveChangesAsync();
        }

        private async Task<int> CalculateProgressAsync(int goalId)
        {
            var results = await _context.KeyResults.AsNoTracking().Where(k => k.GoalId == goalId).ToListAsync();
            return results.Count == 0 ? 0 : (int)Math.Round(results.Average(k => k.TargetValue <= 0 ? 0 : Math.Clamp((double)((k.CurrentValue ?? 0) / k.TargetValue * 100m), 0, 100)));
        }

        private async Task<(List<GoalCardItem> Goals, List<KeyResultEntity> KeyResults)> Load()
        {
            var uid = User.GetUserId();
            var goals = await _context.Goals.AsNoTracking().Where(g => g.UserId == uid).OrderBy(g => g.TargetDate).ToListAsync();
            var ids = goals.Select(g => g.Id).ToArray();
            var krs = await _context.KeyResults.AsNoTracking().Where(k => ids.Contains(k.GoalId)).OrderBy(k => k.Id).ToListAsync();
            var cards = goals.Select(g => new GoalCardItem
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description ?? "",
                Category = g.Category ?? "",
                GoalType = NormalizeType(g.GoalType),
                Status = g.IsCompleted ? GoalStatusKeys.Completed : AllowedStatus(g.Status),
                Progress = Math.Clamp(g.ProgressPercent, 0, 100),
                StartDate = g.StartDate,
                TargetDate = g.TargetDate,
                IsCompleted = g.IsCompleted,
                ReviewNote = g.ReviewNote,
                KeyResultCount = krs.Count(k => k.GoalId == g.Id),
                KeyResultDone = krs.Count(k => k.GoalId == g.Id && k.IsCompleted)
            }).ToList();
            return (cards, krs);
        }

        private static KeyResultRow ToRow(KeyResultEntity k) => new()
        {
            Id = k.Id, GoalId = k.GoalId, Title = k.Title, Current = k.CurrentValue ?? 0, Target = k.TargetValue,
            Unit = k.Unit ?? "", IsCompleted = k.IsCompleted
        };

        private static string AllowedStatus(string? s) => s is GoalStatusKeys.Paused or GoalStatusKeys.Completed ? s : GoalStatusKeys.Active;
        private static string NormalizeType(string? t) => (t ?? "").Contains("long", StringComparison.OrdinalIgnoreCase) || (t ?? "").Contains("dài") ? "long" : "short";

        /// <summary>Nhận khóa hoặc nhãn tiếng Việt, trả về khóa ASCII để lưu varchar.</summary>
        private static string CategoryKey(string? value)
        {
            var v = (value ?? "").Trim();
            var hit = GoalCategories.Labels.FirstOrDefault(x => x.Key == v || x.Value.Equals(v, StringComparison.OrdinalIgnoreCase));
            return hit.Key ?? v;
        }

    }
}
