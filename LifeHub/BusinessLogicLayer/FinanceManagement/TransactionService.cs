
using LifeHub.DataAccessLayer;
using LifeHub.Models.FinanceManagement;
using Microsoft.EntityFrameworkCore;
using System.Data;
using LifeHub.Models.ViewModels.FinanceModules;

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

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Serializable keeps concurrent withdrawals/transfers from both spending the same balance.
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w =>
                w.Id == walletId &&
                w.UserId == userId && w.IsActive
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
                    w.UserId == userId && w.IsActive
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

        SavingsGoal? savingsGoal = null;
        if (savingsGoalId.HasValue)
        {
            savingsGoal = await _context.SavingsGoals
                .FirstOrDefaultAsync(g =>
                    g.Id == savingsGoalId.Value &&
                    g.UserId == userId && !g.IsAchieved
                );

            // A linked goal means a contribution: expense from the selected wallet
            // and an increase to that user's savings goal in the same transaction.
            if (savingsGoal == null || transactionType != "Expense")
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
                if (savingsGoal != null)
                {
                    savingsGoal.CurrentAmount = (savingsGoal.CurrentAmount ?? 0m) + amount;
                    savingsGoal.IsAchieved = savingsGoal.CurrentAmount >= savingsGoal.TargetAmount;
                }
                break;

            case "Transfer":
                wallet.InitialBalance-= amount;
                targetWallet!.InitialBalance += amount;
                break;
        }

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return transaction;
    }

    public async Task<bool> UpdateAsync(int userId, SaveTransactionInput input)
    {
        input.Name=input.Name?.Trim()??"";
        var type=input.Type?.Trim();
        if(input.Amount<=0||string.IsNullOrWhiteSpace(input.Name)||input.Name.Length>255||type is not ("Income" or "Expense")||input.Date==default)return false;
        await using var dbTransaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var current=await _context.Transactions.FirstOrDefaultAsync(t=>t.Id==input.Id&&t.UserId==userId);
        if(current==null)return false;
        var wallets=await _context.Wallets.Where(w=>w.UserId==userId&&(w.Id==current.WalletId||w.Id==current.ToWalletId||w.Id==input.WalletId)).OrderBy(w=>w.Id).ToListAsync();
        var oldSource=wallets.FirstOrDefault(w=>w.Id==current.WalletId); var newSource=wallets.FirstOrDefault(w=>w.Id==input.WalletId);
        var oldTarget=current.ToWalletId.HasValue?wallets.FirstOrDefault(w=>w.Id==current.ToWalletId.Value):null;
        if(oldSource==null||newSource==null||!newSource.IsActive)return false;
        Category? category=null;
        if(input.CategoryId.HasValue&&input.CategoryId.Value>0){category=await _context.Categories.FirstOrDefaultAsync(c=>c.Id==input.CategoryId.Value&&(c.UserId==null||c.UserId==userId));if(category==null||!category.Type.Equals(type,StringComparison.OrdinalIgnoreCase))return false;}
        SavingsGoal? linkedGoal=current.SavingsGoalId.HasValue?await _context.SavingsGoals.FirstOrDefaultAsync(g=>g.Id==current.SavingsGoalId&&g.UserId==userId):null;
        if(current.SavingsGoalId.HasValue&&linkedGoal==null)return false;

        if(current.TransactionType=="Income") oldSource.InitialBalance-=current.Amount;
        else if(current.TransactionType=="Expense") oldSource.InitialBalance+=current.Amount;
        else if(current.TransactionType=="Transfer")
        {
            if(oldTarget==null)return false;
            oldSource.InitialBalance+=current.Amount; oldTarget.InitialBalance-=current.Amount;
        }
        else return false;
        if(linkedGoal!=null){linkedGoal.CurrentAmount=(linkedGoal.CurrentAmount??0)-current.Amount;if(linkedGoal.CurrentAmount<0)return false;linkedGoal.IsAchieved=linkedGoal.CurrentAmount>=linkedGoal.TargetAmount;}
        if(oldSource.InitialBalance<0||(oldTarget?.InitialBalance??0)<0)return false;
        if(type=="Expense"&&newSource.InitialBalance<input.Amount)return false;
        if(type=="Income")newSource.InitialBalance+=input.Amount;else newSource.InitialBalance-=input.Amount;

        current.WalletId=newSource.Id;current.ToWalletId=null;current.CategoryId=category?.Id;current.SavingsGoalId=null;
        current.TransactionType=type;current.Amount=input.Amount;current.TransactionDate=DateOnly.FromDateTime(input.Date);current.Note=input.Name;
        await _context.SaveChangesAsync();await dbTransaction.CommitAsync();return true;
    }

    public async Task<bool> DeleteAsync(int userId,int id)
    {
        await using var dbTransaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var current=await _context.Transactions.FirstOrDefaultAsync(t=>t.Id==id&&t.UserId==userId);if(current==null)return false;
        var source=await _context.Wallets.FirstOrDefaultAsync(w=>w.Id==current.WalletId&&w.UserId==userId);
        var target=current.ToWalletId.HasValue?await _context.Wallets.FirstOrDefaultAsync(w=>w.Id==current.ToWalletId&&w.UserId==userId):null;
        if(source==null||(current.ToWalletId.HasValue&&target==null))return false;
        SavingsGoal? goal=current.SavingsGoalId.HasValue?await _context.SavingsGoals.FirstOrDefaultAsync(g=>g.Id==current.SavingsGoalId&&g.UserId==userId):null;
        if(current.SavingsGoalId.HasValue&&goal==null)return false;
        if(current.TransactionType=="Income")source.InitialBalance-=current.Amount;
        else if(current.TransactionType=="Expense")source.InitialBalance+=current.Amount;
        else if(current.TransactionType=="Transfer"){source.InitialBalance+=current.Amount;target!.InitialBalance-=current.Amount;}
        else return false;
        if(source.InitialBalance<0||(target?.InitialBalance??0)<0)return false;
        if(goal!=null){goal.CurrentAmount=(goal.CurrentAmount??0)-current.Amount;if(goal.CurrentAmount<0)return false;goal.IsAchieved=goal.CurrentAmount>=goal.TargetAmount;}
        _context.Transactions.Remove(current);await _context.SaveChangesAsync();await dbTransaction.CommitAsync();return true;
    }
}
