using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LifeHub.Models.ViewModels.FinanceModules;

namespace LifeHub.Models.MockData
{
    /// <summary>
    /// Nguồn dữ liệu mẫu dùng chung cho Giao dịch, Ví, Mục tiêu tiết kiệm và Báo cáo,
    /// nên số liệu giữa các trang luôn khớp nhau. Ngày tháng tính theo hôm nay (6 tháng gần nhất).
    /// Mọi tổng, tỷ lệ, so sánh đều được TÍNH ở đây (C#), View và JavaScript chỉ hiển thị.
    /// </summary>
    public static class FinanceModulesMockData
    {
        private record Cat(int Id, string Name, string Type, string Color, decimal? Limit);
        private record Wlt(int Id, string Name, string Kind, string Color, decimal Balance);
        private record Txn(int Id, DateTime Date, string Name, string Type, int CategoryId, int WalletId, decimal Amount);
        private record Goal(int Id, string Title, decimal Target, decimal Current, DateTime? Deadline, string Color);

        private static readonly Cat[] Cats =
        {
            new(1, "Ăn uống", "expense", "#f43f5e", 2_500_000m),
            new(2, "Di chuyển", "expense", "#fb923c", 600_000m),
            new(3, "Nhà ở", "expense", "#8b5cf6", 2_800_000m),
            new(4, "Học tập", "expense", "#3b82f6", 800_000m),
            new(5, "Giải trí", "expense", "#ec4899", 500_000m),
            new(6, "Mua sắm", "expense", "#14b8a6", 800_000m),
            new(7, "Sức khỏe", "expense", "#10b981", 400_000m),
            new(8, "Lương", "income", "#10b981", null),
            new(9, "Thưởng", "income", "#f59e0b", null),
            new(10, "Làm thêm", "income", "#06b6d4", null)
        };

        // Số dư của ví lưu sẵn trong mock; riêng "Quỹ tiết kiệm" luôn bằng tổng các mục tiêu.
        private static readonly Wlt[] Wallets =
        {
            new(1, "Tiền mặt", "cash", "#f59e0b", 1_850_000m),
            new(2, "Vietcombank", "bank", "#10b981", 14_200_000m),
            new(3, "Momo", "ewallet", "#ec4899", 920_000m),
            new(4, "Quỹ tiết kiệm", "savings", "#e11d48", 0m)
        };

        // ------------------------------------------------------------------ dữ liệu gốc
        private static List<Txn> BuildTransactions()
        {
            var today = DateTime.Today;
            var list = new List<Txn>();
            var id = 1;

            void Add(DateTime d, string name, string type, int cat, int wallet, decimal amount)
            {
                if (d.Date <= today) list.Add(new Txn(id++, d.Date, name, type, cat, wallet, amount));
            }

            for (var back = 5; back >= 0; back--)
            {
                var first = new DateTime(today.Year, today.Month, 1).AddMonths(-back);
                var rnd = new Random(first.Year * 100 + first.Month);   // cố định để mỗi lần chạy ra cùng số liệu
                decimal Vary(decimal b) => Math.Round(b * (decimal)(0.85 + rnd.NextDouble() * 0.3) / 10_000m) * 10_000m;
                DateTime D(int day) => first.AddDays(Math.Min(day, DateTime.DaysInMonth(first.Year, first.Month)) - 1);

                Add(D(5), "Lương thực tập", "income", 8, 2, 8_000_000m);
                Add(D(15), "Dạy kèm", "income", 10, 2, Vary(2_500_000m));
                if (back % 3 == 0) Add(D(28), "Học bổng", "income", 9, 2, 3_000_000m);

                Add(D(3), "Tiền thuê phòng", "expense", 3, 2, 2_500_000m);
                Add(D(8), "Điện nước, internet", "expense", 3, 2, Vary(450_000m));
                foreach (var day in new[] { 6, 13, 20, 27 }) Add(D(day), "Đi chợ, siêu thị", "expense", 1, 1, Vary(400_000m));
                foreach (var day in new[] { 4, 11, 18, 25 }) Add(D(day), "Cà phê, ăn ngoài", "expense", 1, 3, Vary(150_000m));
                Add(D(7), "Xăng xe", "expense", 2, 1, Vary(200_000m));
                Add(D(21), "Grab, xe buýt", "expense", 2, 3, Vary(200_000m));
                Add(D(12), "Sách, khóa học online", "expense", 4, 2, Vary(350_000m));
                Add(D(15), "Xem phim, game", "expense", 5, 3, Vary(200_000m));
                if (back % 2 == 1) Add(D(17), "Mua quần áo", "expense", 6, 2, Vary(500_000m));
                Add(D(23), "Thuốc, khám sức khỏe", "expense", 7, 1, Vary(150_000m));
            }

            return list.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
        }

        private static List<Goal> BuildGoals()
        {
            var today = DateTime.Today;
            return new List<Goal>
            {
                new(1, "Quỹ khẩn cấp", 20_000_000m, 9_500_000m, today.AddDays(240), "#e11d48"),
                new(2, "Mua laptop mới", 18_000_000m, 14_000_000m, today.AddDays(75), "#3b82f6"),
                new(3, "Du lịch Đà Lạt", 5_000_000m, 5_000_000m, today.AddDays(-12), "#10b981"),
                new(4, "Học IELTS", 8_000_000m, 2_400_000m, today.AddDays(300), "#f59e0b")
            };
        }

        // ------------------------------------------------------------------ chuyển đổi
        private static TransactionRow ToRow(Txn t)
        {
            var c = Cats.First(x => x.Id == t.CategoryId);
            var w = Wallets.First(x => x.Id == t.WalletId);
            return new TransactionRow
            {
                Id = t.Id, Date = t.Date, Name = t.Name, Type = t.Type,
                CategoryId = c.Id, CategoryName = c.Name, CategoryColor = c.Color,
                WalletId = w.Id, WalletName = w.Name, Amount = t.Amount
            };
        }

        private static List<CategoryOption> CategoryOptions() => Cats
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name, Type = c.Type, Color = c.Color, MonthlyLimit = c.Limit })
            .ToList();

        private static decimal SavingsTotal() => BuildGoals().Sum(g => g.Current);

        private static decimal WalletBalance(Wlt w) => w.Kind == "savings" ? SavingsTotal() : w.Balance;

        private static List<WalletOption> WalletOptions(bool includeSavings) => Wallets
            .Where(w => includeSavings || w.Kind != "savings")
            .Select(w => new WalletOption { Id = w.Id, Name = w.Name, Kind = w.Kind, Balance = WalletBalance(w) })
            .ToList();

        private static List<MonthOption> MonthOptions()
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return Enumerable.Range(0, 6)
                .Select(i => start.AddMonths(-i))
                .Select(d => new MonthOption { Key = d.ToString("yyyy-MM"), Label = $"Tháng {d.Month:00}/{d.Year}" })
                .ToList();
        }

        // ------------------------------------------------------------------ Giao dịch
        public static TransactionPageViewModel GetTransactionPage(TransactionFilter filter)
        {
            var thisMonth = DateTime.Today.ToString("yyyy-MM");
            var effectiveMonth = filter.HasDateRange ? "all"
                : string.IsNullOrWhiteSpace(filter.Month) ? thisMonth : filter.Month!;
            var q = FinanceFormat.Fold(filter.Q);

            IEnumerable<Txn> rows = BuildTransactions();
            if (filter.Type is "income" or "expense") rows = rows.Where(t => t.Type == filter.Type);
            if (filter.CategoryId.HasValue) rows = rows.Where(t => t.CategoryId == filter.CategoryId.Value);
            if (filter.WalletId.HasValue) rows = rows.Where(t => t.WalletId == filter.WalletId.Value);
            if (effectiveMonth != "all") rows = rows.Where(t => t.Date.ToString("yyyy-MM") == effectiveMonth);
            if (filter.From.HasValue) rows = rows.Where(t => t.Date >= filter.From.Value.Date);
            if (filter.To.HasValue) rows = rows.Where(t => t.Date <= filter.To.Value.Date);
            if (q.Length > 0)
            {
                rows = rows.Where(t => FinanceFormat.Fold(t.Name).Contains(q)
                    || FinanceFormat.Fold(Cats.First(c => c.Id == t.CategoryId).Name).Contains(q));
            }

            string period;
            if (filter.HasDateRange)
            {
                var from = filter.From.HasValue ? FinanceFormat.Date(filter.From.Value) : "đầu";
                var to = filter.To.HasValue ? FinanceFormat.Date(filter.To.Value) : "nay";
                period = $"Từ {from} đến {to}";
            }
            else period = effectiveMonth == "all" ? "Tất cả thời gian" : FinanceFormat.MonthLabel(effectiveMonth);

            return new TransactionPageViewModel
            {
                Filter = filter,
                EffectiveMonth = effectiveMonth,
                PeriodLabel = period,
                Rows = rows.Select(ToRow).ToList(),
                Categories = CategoryOptions(),
                Wallets = WalletOptions(includeSavings: false),
                Months = MonthOptions()
            };
        }

        // ------------------------------------------------------------------ Ví
        public static WalletPageViewModel GetWalletPage()
        {
            var all = BuildTransactions();
            var monthKey = DateTime.Today.ToString("yyyy-MM");

            return new WalletPageViewModel
            {
                Wallets = Wallets.Select(w =>
                {
                    var own = all.Where(t => t.WalletId == w.Id).ToList();
                    var thisMonth = own.Where(t => t.Date.ToString("yyyy-MM") == monthKey).ToList();
                    return new WalletCard
                    {
                        Id = w.Id, Name = w.Name, Kind = w.Kind, Color = w.Color,
                        Balance = WalletBalance(w),
                        MonthIncome = thisMonth.Where(t => t.Type == "income").Sum(t => t.Amount),
                        MonthExpense = thisMonth.Where(t => t.Type == "expense").Sum(t => t.Amount),
                        Recent = own.Take(3).Select(ToRow).ToList()
                    };
                }).ToList()
            };
        }

        // ------------------------------------------------------------------ Mục tiêu tiết kiệm
        public static SavingGoalPageViewModel GetSavingGoalPage(string? status)
        {
            status = (status is "achieved" or "all") ? status : "active";

            var cards = BuildGoals().Select(g => new SavingGoalCard
            {
                Id = g.Id, Title = g.Title, TargetAmount = g.Target, CurrentAmount = g.Current,
                Deadline = g.Deadline, Color = g.Color
            }).ToList();

            return new SavingGoalPageViewModel
            {
                Status = status,
                Goals = cards.Where(c => status == "all" || (status == "achieved" ? c.IsAchieved : !c.IsAchieved)).ToList(),
                SourceWallets = WalletOptions(includeSavings: false),
                TotalSaved = cards.Sum(c => c.CurrentAmount),
                TotalTarget = cards.Sum(c => c.TargetAmount),
                ActiveCount = cards.Count(c => !c.IsAchieved),
                AchievedCount = cards.Count(c => c.IsAchieved)
            };
        }

        // ------------------------------------------------------------------ Báo cáo
        public static FinanceReportViewModel GetReport(string? month)
        {
            var all = BuildTransactions();
            var currentStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var earliest = currentStart.AddMonths(-5);

            if (!DateTime.TryParseExact((month ?? "") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sel))
                sel = currentStart;
            if (sel > currentStart) sel = currentStart;
            if (sel < earliest) sel = earliest;

            decimal Sum(DateTime start, string type) => all
                .Where(t => t.Type == type && t.Date.Year == start.Year && t.Date.Month == start.Month)
                .Sum(t => t.Amount);
            decimal? Change(decimal now, decimal before) => before == 0 ? null : (now - before) / before * 100m;

            var income = Sum(sel, "income");
            var expense = Sum(sel, "expense");
            var prev = sel.AddMonths(-1);

            // Biểu đồ 6 tháng: cột cao nhất = 100%
            var trend = Enumerable.Range(0, 6).Select(i => earliest.AddMonths(i)).Select(d => new ReportMonthPoint
            {
                Key = d.ToString("yyyy-MM"), Label = $"T{d.Month}",
                Income = Sum(d, "income"), Expense = Sum(d, "expense"), IsSelected = d == sel
            }).ToList();
            var max = Math.Max(1m, trend.Max(p => Math.Max(p.Income, p.Expense)));
            foreach (var p in trend)
            {
                p.IncomeHeight = p.Income > 0 ? Math.Max(2, (double)(p.Income / max * 100m)) : 0;
                p.ExpenseHeight = p.Expense > 0 ? Math.Max(2, (double)(p.Expense / max * 100m)) : 0;
            }

            // Chi tiêu theo danh mục
            var monthExpenses = all.Where(t => t.Type == "expense" && t.Date.Year == sel.Year && t.Date.Month == sel.Month).ToList();
            var slices = monthExpenses.GroupBy(t => t.CategoryId)
                .Select(g =>
                {
                    var c = Cats.First(x => x.Id == g.Key);
                    var amount = g.Sum(t => t.Amount);
                    return new ReportCategorySlice
                    {
                        Name = c.Name, Color = c.Color, Amount = amount,
                        Percent = expense <= 0 ? 0 : Math.Round((double)(amount / expense * 100m), 1)
                    };
                })
                .OrderByDescending(s => s.Amount).ToList();

            var stops = new List<string>();
            double acc = 0;
            foreach (var s in slices)
            {
                var end = Math.Min(100, acc + s.Percent);
                stops.Add($"{s.Color} {FinanceFormat.Css(acc)}% {FinanceFormat.Css(end)}%");
                acc = end;
            }

            // So sánh ngân sách (hạn mức) với thực chi
            var budgets = Cats.Where(c => c.Type == "expense" && c.Limit.HasValue)
                .Select(c => new ReportBudgetRow
                {
                    Name = c.Name, Color = c.Color, Limit = c.Limit!.Value,
                    Spent = monthExpenses.Where(t => t.CategoryId == c.Id).Sum(t => t.Amount)
                })
                .OrderByDescending(b => b.Percent).ToList();

            var report = new FinanceReportViewModel
            {
                Month = sel.ToString("yyyy-MM"),
                PrevMonth = sel > earliest ? prev.ToString("yyyy-MM") : null,
                NextMonth = sel < currentStart ? sel.AddMonths(1).ToString("yyyy-MM") : null,
                Income = income,
                Expense = expense,
                IncomeChange = Change(income, Sum(prev, "income")),
                ExpenseChange = Change(expense, Sum(prev, "expense")),
                Trend = trend,
                Slices = slices,
                DonutGradient = stops.Count > 0 ? "conic-gradient(" + string.Join(", ", stops) + ")" : "conic-gradient(var(--finance-soft) 0% 100%)",
                BudgetRows = budgets,
                TopExpenses = monthExpenses.OrderByDescending(t => t.Amount).Take(5).Select(ToRow).ToList()
            };

            // Nhận xét tự động
            if (report.ExpenseChange.HasValue)
            {
                var pct = Math.Abs(Math.Round(report.ExpenseChange.Value, 1));
                report.Insights.Add(report.ExpenseChange.Value >= 0
                    ? $"Chi tiêu tăng {pct.ToString(FinanceFormat.Vi)}% so với tháng trước."
                    : $"Chi tiêu giảm {pct.ToString(FinanceFormat.Vi)}% so với tháng trước.");
            }
            foreach (var over in budgets.Where(b => b.State == "over"))
                report.Insights.Add($"Danh mục {over.Name} vượt hạn mức {FinanceFormat.Money(-over.Diff)}.");
            if (income > 0)
                report.Insights.Add($"Bạn giữ lại {report.SavingsRate.ToString(FinanceFormat.Vi)}% thu nhập của tháng này.");

            return report;
        }
    }
}