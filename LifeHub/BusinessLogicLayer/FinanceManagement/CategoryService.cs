
using LifeHub.DataAccessLayer;
using LifeHub.Models.FinanceManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.BusinessLogicLayer.FinanceManagement;

public class CategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync(int userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id, int userId)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (c.UserId == null || c.UserId == userId)
            );
    }
    
    public async Task<Category?> CreateAsync(
        int userId,
        string name,
        string type,
        string? icon,
        string? color,
        int? parentId)
    {
        name = name.Trim();
        type = type.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(type))
            return null;

        if (!await IsValidParentAsync(parentId, userId, type))
            return null;

        bool duplicate = await _context.Categories.AnyAsync(c =>
            (c.UserId == null || c.UserId == userId) &&
            c.Name == name &&
            c.Type == type &&
            c.ParentId == parentId
        );

        if (duplicate)
            return null;

        var category = new Category
        {
            UserId = userId,
            ParentId = parentId,
            Name = name,
            Type = type,
            Icon = icon,
            Color = color,
            IsDefault = false
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return category;
    }

    public async Task<bool> UpdateAsync(
        int id,
        int userId,
        string name,
        string type,
        string? icon,
        string? color,
        int? parentId)
    {
        name = name.Trim();
        type = type.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(type))
            return false;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.UserId == userId &&
                !c.IsDefault
            );

        if (category == null)
            return false;

        if (parentId == id)
            return false;

        if (!await IsValidParentAsync(parentId, userId, type))
            return false;

        bool duplicate = await _context.Categories.AnyAsync(c =>
            c.Id != id &&
            (c.UserId == null || c.UserId == userId) &&
            c.Name == name &&
            c.Type == type &&
            c.ParentId == parentId
        );

        if (duplicate)
            return false;

        category.Name = name;
        category.Type = type;
        category.Icon = icon;
        category.Color = color;
        category.ParentId = parentId;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.UserId == userId &&
                !c.IsDefault
            );

        if (category == null)
            return false;

        bool hasChildren = await _context.Categories
            .AnyAsync(c => c.ParentId == id);

        if (hasChildren)
            return false;

        bool hasBudgets = await _context.Budgets
            .AnyAsync(b => b.CategoryId == id);

        if (hasBudgets)
            return false;

        var transactions = await _context.Transactions
            .Where(t => t.CategoryId == id)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.CategoryId = null;
        }

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> IsValidParentAsync(
        int? parentId,
        int userId,
        string type)
    {
        if (parentId == null)
            return true;

        var parent = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == parentId &&
                (c.UserId == null || c.UserId == userId)
            );

        if (parent == null)
            return false;

        return parent.Type == type;
    }
}
