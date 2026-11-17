
using LifeHub.DataAccessLayer;
using LifeHub.Models.FinanceManagement;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.BusinessLogicLayer.FinanceManagement;

public class TransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> CreateAsync(
        int userId,
        string transactionType,
        int walletId,
        int? toWalletId,
        int? categoryId,
        int? savingsGoalId,
        decimal amount,
        DateOnly transactionDate,
        string? note)
    {
        if (amount <= 0)
            return null;

        transactionType = transactionType.Trim();

        if (transactionType != "Income" &&
            transactionType != "Expense" &&
            transactionType != "Transfer")
            return null;

        // Kiểm tra ví nguồn
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w =>
                w.Id == walletId &&
                w.UserId == userId
            );

        if (wallet == null)
            return null;

        Wallet? targetWallet = null;

        // Kiểm tra ví nhận khi chuyển tiền
        if (transactionType == "Transfer")
        {
            if (toWalletId == null || toWalletId == walletId)
                return null;

            targetWallet = await _context.Wallets
                .FirstOrDefaultAsync(w =>
                    w.Id == toWalletId &&
                    w.UserId == userId
                );

            if (targetWallet == null)
                return null;

            if (wallet.InitialBalance < amount)
                return null;
        }
        else
        {
            if (toWalletId != null)
                return null;

            if (transactionType == "Expense" &&
                wallet.InitialBalance < amount)
                return null;
        }

        // Kiểm tra danh mục
        if (categoryId.HasValue)
        {
            if (transactionType == "Transfer")
                return null;

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == categoryId.Value &&
                    (c.UserId == null || c.UserId == userId)
                );

            if (category == null ||
                !string.Equals(
                    category.Type,
                    transactionType,
                    StringComparison.OrdinalIgnoreCase
                ))
                return null;
        }

        // Kiểm tra mục tiêu tiết kiệm
        if (savingsGoalId.HasValue)
        {
            var goal = await _context.SavingsGoals
                .FirstOrDefaultAsync(g =>
                    g.Id == savingsGoalId.Value &&
                    g.UserId == userId
                );

            if (goal == null)
                return null;
        }

        // Tạo giao dịch
        var transaction = new Transaction
        {
            UserId = userId,
            WalletId = walletId,
            ToWalletId = toWalletId,
            CategoryId = categoryId,
            SavingsGoalId = savingsGoalId,
            TransactionType = transactionType,
            Amount = amount,
            TransactionDate = transactionDate,
            Note = note?.Trim(),
            CreatedAt = DateTime.Now
        };

        // Cập nhật số dư ví
        switch (transactionType)
        {
            case "Income":
                wallet.InitialBalance += amount;
                break;

            case "Expense":
                wallet.InitialBalance -= amount;
                break;

            case "Transfer":
                wallet.InitialBalance-= amount;
                targetWallet!.InitialBalance += amount;
                break;
        }

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        return transaction;
    }
}
