using DrinkStore.Areas.Admin.ViewModels;
using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ShiftManager")]
public class RecipesController : Controller
{
    private readonly DrinkStoreDb _db;
    public RecipesController(DrinkStoreDb db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Recipes.AsNoTracking()
            .Include(r => r.MenuItem).Include(r => r.RecipeItems)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.MenuItem.Name.Contains(search.Trim()));
        ViewBag.Search = search;
        return View(await query.OrderBy(r => r.MenuItem.Name)
            .ThenBy(r => r.Size).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var recipe = await _db.Recipes.AsNoTracking()
            .Include(r => r.MenuItem)
            .Include(r => r.RecipeItems).ThenInclude(i => i.Ingredient)
            .FirstOrDefaultAsync(r => r.RecipeId == id);
        return recipe == null ? NotFound() : View(recipe);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new RecipeFormViewModel();
        await PopulateOptions(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeFormViewModel vm)
    {
        await ValidateRecipe(vm);
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View("Form", vm);
        }
        var recipe = new Recipe
        {
            MenuItemId = vm.MenuItemId,
            Size = vm.Size,
            Instructions = vm.Instructions?.Trim() ?? "",
            RecipeItems = vm.Ingredients.Select(x => new RecipeItem
            {
                IngredientId = x.IngredientId,
                RequiredAmount = x.RequiredAmount
            }).ToList()
        };
        // Đơn vị được lấy từ Ingredient, không tin giá trị client gửi lên.
        await AssignUnits(recipe.RecipeItems);
        _db.Recipes.Add(recipe);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã tạo công thức.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không lưu được công thức (có thể trùng đồ uống và size).");
            await PopulateOptions(vm);
            return View("Form", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var recipe = await _db.Recipes.AsNoTracking()
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == id);
        if (recipe == null) return NotFound();
        var vm = new RecipeFormViewModel
        {
            RecipeId = recipe.RecipeId,
            MenuItemId = recipe.MenuItemId,
            Size = recipe.Size,
            Instructions = recipe.Instructions,
            Ingredients = recipe.RecipeItems.OrderBy(x => x.IngredientId)
                .Select(x => new RecipeIngredientInput
                {
                    IngredientId = x.IngredientId,
                    RequiredAmount = x.RequiredAmount
                }).ToList()
        };
        await PopulateOptions(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RecipeFormViewModel vm)
    {
        if (id != vm.RecipeId) return BadRequest();
        await ValidateRecipe(vm, id);
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View("Form", vm);
        }
        var recipe = await _db.Recipes.Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == id);
        if (recipe == null) return NotFound();
        recipe.MenuItemId = vm.MenuItemId;
        recipe.Size = vm.Size;
        recipe.Instructions = vm.Instructions?.Trim() ?? "";
        _db.RecipeItems.RemoveRange(recipe.RecipeItems);
        recipe.RecipeItems = vm.Ingredients.Select(x => new RecipeItem
        {
            RecipeId = id,
            IngredientId = x.IngredientId,
            RequiredAmount = x.RequiredAmount
        }).ToList();
        await AssignUnits(recipe.RecipeItems);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật công thức.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không lưu được công thức. Kiểm tra dữ liệu hoặc công thức trùng.");
            await PopulateOptions(vm);
            return View("Form", vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var recipe = await _db.Recipes.Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.RecipeId == id);
        if (recipe == null) return NotFound();
        _db.RecipeItems.RemoveRange(recipe.RecipeItems);
        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa công thức.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptions(RecipeFormViewModel vm)
    {
        vm.MenuOptions = await _db.MenuItems.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.ItemId.ToString(), Text = x.Name })
            .ToListAsync();
        vm.IngredientOptions = await _db.Ingredients.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.IngredientId.ToString(), Text = x.Name + " (" + x.Unit + ")"
            }).ToListAsync();
    }

    private async Task ValidateRecipe(RecipeFormViewModel vm, int? excludingId = null)
    {
        if (!await _db.MenuItems.AnyAsync(x => x.ItemId == vm.MenuItemId))
            ModelState.AddModelError(nameof(vm.MenuItemId), "Đồ uống không tồn tại.");
        if (await _db.Recipes.AnyAsync(x => x.MenuItemId == vm.MenuItemId
                && x.Size == vm.Size && (!excludingId.HasValue || x.RecipeId != excludingId.Value)))
            ModelState.AddModelError("", "Đã tồn tại công thức cho đồ uống và size này.");
        if (vm.Ingredients.Count == 0)
            ModelState.AddModelError("", "Cần ít nhất một nguyên liệu.");
        if (vm.Ingredients.Count > 50)
            ModelState.AddModelError("", "Tối đa 50 nguyên liệu trong một công thức.");
        if (vm.Ingredients.Any(x => x.IngredientId <= 0 || x.RequiredAmount <= 0 || x.RequiredAmount > 999999999))
            ModelState.AddModelError("", "Nguyên liệu và định lượng phải hợp lệ, lớn hơn 0.");
        var ids = vm.Ingredients.Select(x => x.IngredientId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            ModelState.AddModelError("", "Không được chọn trùng nguyên liệu.");
        if (await _db.Ingredients.CountAsync(x => ids.Contains(x.IngredientId)) != ids.Distinct().Count())
            ModelState.AddModelError("", "Có nguyên liệu không tồn tại.");
    }

    private async Task AssignUnits(IEnumerable<RecipeItem> items)
    {
        var ids = items.Select(x => x.IngredientId).ToList();
        var units = await _db.Ingredients.AsNoTracking()
            .Where(x => ids.Contains(x.IngredientId))
            .ToDictionaryAsync(x => x.IngredientId, x => x.Unit);
        foreach (var item in items) item.Unit = units[item.IngredientId];
    }
}
