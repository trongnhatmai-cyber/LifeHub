
using System;
using System.Collections.Generic;
using LifeHub.Models.ViewModels.FinanceManagement;

namespace LifeHub.Models.MockData
{
    public static class FinanceMockData
    {
        public static FinanceViewModel GetData()
        {
            return new FinanceViewModel
            {
                // Tổng quan tài chính
                TotalIncome = 25000000m,
                TotalExpense = 12800000m,
                TotalSaving = 5000000m,

                // Dữ liệu tương thích với các thuộc tính cũ
                MonthlyBudget = 15000000m,
                SavingGoal = 20000000m,
                SavingProgress = 5000000m,

                // Ngân sách tổng của tháng và hạn mức từng danh mục
                MonthlyBudgetDetails = new MonthlyBudgetViewModel
                {
                    Month = 11,
                    Year = 2026,
                    Limit = 15000000m,
                    Spent = 12800000m,

                    CategoryBudgets = new List<BudgetViewModel>
                    {
                        new BudgetViewModel
                        {
                            Id = 1,
                            CategoryId = 1,
                            Category = "Ăn uống",
                            Limit = 5000000m,
                            Spent = 4500000m,
                            Month = 11,
                            Year = 2026
                        },
                        new BudgetViewModel
                        {
                            Id = 2,
                            CategoryId = 2,
                            Category = "Di chuyển",
                            Limit = 1500000m,
                            Spent = 1200000m,
                            Month = 11,
                            Year = 2026
                        },
                        new BudgetViewModel
                        {
                            Id = 3,
                            CategoryId = 3,
                            Category = "Học tập",
                            Limit = 3000000m,
                            Spent = 2100000m,
                            Month = 11,
                            Year = 2026
                        },
                        new BudgetViewModel
                        {
                            Id = 4,
                            CategoryId = 4,
                            Category = "Mua sắm",
                            Limit = 5500000m,
                            Spent = 5000000m,
                            Month = 11,
                            Year = 2026
                        }
                    }
                },

                // Danh mục thu và chi
                Categories = new List<FinanceCategoryViewModel>
                {
                    new FinanceCategoryViewModel
                    {
                        Id = 1,
                        Name = "Ăn uống",
                        Type = "Expense",
                        Spent = 4500000m,
                        Limit = 5000000m
                    },
                    new FinanceCategoryViewModel
                    {
                        Id = 2,
                        Name = "Di chuyển",
                        Type = "Expense",
                        Spent = 1200000m,
                        Limit = 1500000m
                    },
                    new FinanceCategoryViewModel
                    {
                        Id = 3,
                        Name = "Học tập",
                        Type = "Expense",
                        Spent = 2100000m,
                        Limit = 3000000m
                    },
                    new FinanceCategoryViewModel
                    {
                        Id = 4,
                        Name = "Mua sắm",
                        Type = "Expense",
                        Spent = 5000000m,
                        Limit = 5500000m
                    },
                    new FinanceCategoryViewModel
                    {
                        Id = 5,
                        Name = "Lương",
                        Type = "Income",
                        Spent = 20000000m,
                        Limit = 0m
                    },
                    new FinanceCategoryViewModel
                    {
                        Id = 6,
                        Name = "Thưởng",
                        Type = "Income",
                        Spent = 5000000m,
                        Limit = 0m
                    }
                },

                // Giao dịch mẫu để hiển thị trong lịch sử
                Transactions = new List<FinanceTransactionViewModel>
                {
                    new FinanceTransactionViewModel
                    {
                        Id = 1,
                        Name = "Lương tháng 11",
                        Type = "Income",
                        CategoryId = 5,
                        Category = "Lương",
                        Date = new DateTime(2026, 11, 1),
                        Amount = 20000000m
                    },
                    new FinanceTransactionViewModel
                    {
                        Id = 2,
                        Name = "Thưởng dự án",
                        Type = "Income",
                        CategoryId = 6,
                        Category = "Thưởng",
                        Date = new DateTime(2026, 11, 5),
                        Amount = 5000000m
                    },
                    new FinanceTransactionViewModel
                    {
                        Id = 3,
                        Name = "Ăn trưa",
                        Type = "Expense",
                        CategoryId = 1,
                        Category = "Ăn uống",
                        Date = new DateTime(2026, 11, 10),
                        Amount = 85000m
                    },
                    new FinanceTransactionViewModel
                    {
                        Id = 4,
                        Name = "Tiền xe buýt",
                        Type = "Expense",
                        CategoryId = 2,
                        Category = "Di chuyển",
                        Date = new DateTime(2026, 11, 12),
                        Amount = 30000m
                    },
                    new FinanceTransactionViewModel
                    {
                        Id = 5,
                        Name = "Mua sách",
                        Type = "Expense",
                        CategoryId = 3,
                        Category = "Học tập",
                        Date = new DateTime(2026, 11, 13),
                        Amount = 250000m
                    },
                    new FinanceTransactionViewModel
                    {
                        Id = 6,
                        Name = "Mua đồ dùng cá nhân",
                        Type = "Expense",
                        CategoryId = 4,
                        Category = "Mua sắm",
                        Date = new DateTime(2026, 11, 14),
                        Amount = 420000m
                    }
                },

                // Mục tiêu tiết kiệm
                SavingGoals = new List<SavingGoalViewModel>
                {
                    new SavingGoalViewModel
                    {
                        Id = 1,
                        Title = "Mua laptop",
                        TargetAmount = 20000000m,
                        CurrentAmount = 5000000m,
                        Deadline = new DateTime(2027, 3, 31)
                    },
                    new SavingGoalViewModel
                    {
                        Id = 2,
                        Title = "Quỹ dự phòng",
                        TargetAmount = 30000000m,
                        CurrentAmount = 10000000m,
                        Deadline = new DateTime(2027, 6, 30)
                    }
                },

                // Dữ liệu biểu đồ theo tháng và theo tuần
                CashFlow = new CashFlowViewModel
                {
                    MonthlyData = new List<CashFlowPointViewModel>
                    {
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 6, 1),
                            Label = "T6",
                            Income = 18000000m,
                            Expense = 11000000m
                        },
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 7, 1),
                            Label = "T7",
                            Income = 20000000m,
                            Expense = 12500000m
                        },
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 8, 1),
                            Label = "T8",
                            Income = 22000000m,
                            Expense = 14000000m
                        },
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 9, 1),
                            Label = "T9",
                            Income = 21000000m,
                            Expense = 13000000m
                        },
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 10, 1),
                            Label = "T10",
                            Income = 23000000m,
                            Expense = 13500000m
                        },
                        new CashFlowPointViewModel
                        {
                            Date = new DateTime(2026, 11, 1),
                            Label = "T11",
                            Income = 25000000m,
                            Expense = 12800000m
                        }
                    },

                    WeeklyData = new List<CashFlowWeekViewModel>
                    {
                        CreateWeek(
                            new DateTime(2026, 10, 26),
                            new[]
                            {
                                (0m, 0m), (500000m, 150000m),
                                (0m, 200000m), (0m, 100000m),
                                (0m, 250000m), (0m, 300000m),
                                (0m, 100000m)
                            }
                        ),

                        CreateWeek(
                            new DateTime(2026, 11, 2),
                            new[]
                            {
                                (20000000m, 0m), (0m, 350000m),
                                (0m, 200000m), (0m, 180000m),
                                (5000000m, 250000m), (0m, 300000m),
                                (0m, 150000m)
                            }
                        ),

                        CreateWeek(
                            new DateTime(2026, 11, 9),
                            new[]
                            {
                                (0m, 85000m), (0m, 120000m),
                                (0m, 30000m), (0m, 250000m),
                                (0m, 420000m), (0m, 180000m),
                                (0m, 200000m)
                            }
                        ),

                        CreateWeek(
                            new DateTime(2026, 11, 16),
                            new[]
                            {
                                (0m, 150000m), (0m, 200000m),
                                (0m, 100000m), (0m, 300000m),
                                (0m, 250000m), (0m, 180000m),
                                (0m, 120000m)
                            }
                        ),

                        CreateWeek(
                            new DateTime(2026, 11, 23),
                            new[]
                            {
                                (0m, 100000m), (0m, 250000m),
                                (0m, 150000m), (0m, 200000m),
                                (0m, 300000m), (0m, 180000m),
                                (0m, 220000m)
                            }
                        )
                    }
                }
            };
        }

        private static CashFlowWeekViewModel CreateWeek(
            DateTime weekStart,
            (decimal Income, decimal Expense)[] values)
        {
            var days = new List<CashFlowPointViewModel>();
            var labels = new[]
            {
                "T2", "T3", "T4", "T5", "T6", "T7", "CN"
            };

            for (int i = 0; i < values.Length; i++)
            {
                var date = weekStart.AddDays(i);

                days.Add(new CashFlowPointViewModel
                {
                    Date = date,
                    Label = labels[i],
                    Income = values[i].Income,
                    Expense = values[i].Expense
                });
            }

            return new CashFlowWeekViewModel
            {
                WeekStart = weekStart,
                WeekEnd = weekStart.AddDays(6),
                Label = $"{weekStart:dd/MM} - {weekStart.AddDays(6):dd/MM}",
                Days = days
            };
        }
    }
}
