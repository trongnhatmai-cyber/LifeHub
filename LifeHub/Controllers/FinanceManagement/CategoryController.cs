
using LifeHub.BusinessLogicLayer.FinanceManagement;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.FinanceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers.FinanceManagement;

[Authorize]
public class CategoryController : Controller
{
    private readonly CategoryService _categoryService;

    public CategoryController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET: /Category
    public async Task<IActionResult> Index()
    {
        int userId = User.GetUserId();

        var categories = await _categoryService.GetAllAsync(userId);

        return View(categories);
    }

    // GET: /Category/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int userId = User.GetUserId();

        var category = await _categoryService.CreateAsync(
            userId,
            model.Name,
            model.Type,
            model.Icon,
            model.Color,
            model.ParentId
        );

        if (category == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể tạo danh mục. Vui lòng kiểm tra dữ liệu hoặc tên danh mục đã tồn tại."
            );

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Category/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        int userId = User.GetUserId();

        var category = await _categoryService.GetByIdAsync(id, userId);

        if (category == null ||
            category.UserId != userId ||
            category.IsDefault)
        {
            return NotFound();
        }

        var model = new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color,
            ParentId = category.ParentId
        };

        return View(model);
    }

    // POST: /Category/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int userId = User.GetUserId();

        bool success = await _categoryService.UpdateAsync(
            model.Id,
            userId,
            model.Name,
            model.Type,
            model.Icon,
            model.Color,
            model.ParentId
        );

        if (!success)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật danh mục. Vui lòng kiểm tra dữ liệu."
            );

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Category/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = User.GetUserId();

        bool success = await _categoryService.DeleteAsync(id, userId);

        if (!success)
            return BadRequest("Không thể xóa danh mục này.");

        return RedirectToAction(nameof(Index));
    }
}
