using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ShiftManager")]
public class IngredientsController(DrinkStoreDb db) : Controller
{
    public async Task<IActionResult> Index(string? search, bool lowStock = false)
    {
        var query = db.Ingredients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search.Trim()));
        if (lowStock)
            query = query.Where(x => x.OnHand - x.Reserved <= x.ReorderThreshold);
        ViewBag.Search = search;
        ViewBag.LowStock = lowStock;
        return View(await query.OrderBy(x => x.Name).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Ingredient());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Unit,ReorderThreshold")] Ingredient input)
    {
        Validate(input);
        if (!ModelState.IsValid) return View(input);
        db.Ingredients.Add(new Ingredient
        {
            Name = input.Name.Trim(), Unit = input.Unit.Trim(),
            ReorderThreshold = input.ReorderThreshold, OnHand = 0, Reserved = 0
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm nguyên liệu. Hãy dùng chức năng nhập kho để thêm số lượng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Ingredients.AsNoTracking().FirstOrDefaultAsync(x => x.IngredientId == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Unit,ReorderThreshold")] Ingredient input)
    {
        Validate(input);
        if (!ModelState.IsValid)
        {
            input.IngredientId = id;
            return View(input);
        }
        var item = await db.Ingredients.FindAsync(id);
        if (item is null) return NotFound();
        // Đổi đơn vị khi đã có tồn kho/công thức có thể làm sai định lượng.
        if (item.Unit != input.Unit.Trim() &&
            (item.OnHand != 0 || item.Reserved != 0 ||
             await db.RecipeItems.AnyAsync(x => x.IngredientId == id)))
        {
            ModelState.AddModelError(nameof(input.Unit), "Không thể đổi đơn vị khi nguyên liệu đã có tồn kho hoặc công thức.");
            input.IngredientId = id;
            return View(input);
        }
        item.Name = input.Name.Trim();
        item.Unit = input.Unit.Trim();
        item.ReorderThreshold = input.ReorderThreshold;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật nguyên liệu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> StockIn(int id)
    {
        var item = await db.Ingredients.AsNoTracking().FirstOrDefaultAsync(x => x.IngredientId == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("StockIn"), ValidateAntiForgeryToken]
    public async Task<IActionResult> StockInPost(int id, decimal amount)
    {
        if (amount <= 0 || decimal.Round(amount, 3) != amount || amount > 1000000000m)
        {
            TempData["Error"] = "Số lượng nhập phải lớn hơn 0, tối đa 3 chữ số thập phân.";
            return RedirectToAction(nameof(StockIn), new { id });
        }
        // Cập nhật tại DB để tránh mất số lượng khi có nhiều lượt nhập kho đồng thời.
        var affected = await db.Ingredients.Where(x => x.IngredientId == id && x.OnHand <= 999999999999999m - amount)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OnHand, x => x.OnHand + amount));
        if (affected == 0) return NotFound();
        TempData["Success"] = "Nhập kho thành công.";
        return RedirectToAction(nameof(Index));
    }

    private void Validate(Ingredient item)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 150)
            ModelState.AddModelError(nameof(item.Name), "Tên nguyên liệu phải có từ 1 đến 150 ký tự.");
        if (string.IsNullOrWhiteSpace(item.Unit) || item.Unit.Trim().Length > 30)
            ModelState.AddModelError(nameof(item.Unit), "Đơn vị tính phải có từ 1 đến 30 ký tự.");
        if (item.ReorderThreshold < 0 || decimal.Round(item.ReorderThreshold, 3) != item.ReorderThreshold)
            ModelState.AddModelError(nameof(item.ReorderThreshold), "Ngưỡng cảnh báo phải không âm và tối đa 3 chữ số thập phân.");
    }
}
