using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Services
{
    // Dùng bên trong transaction của OrdersController.
    public class InventoryService
    {
        private readonly DrinkStoreDb _context;
        public InventoryService(DrinkStoreDb context) => _context = context;

        public async Task<(bool Success, string Error)> DeductForOrderAsync(int orderId)
        {
            var items = await _context.OrderItems.AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .Select(x => new { x.MenuItemId, x.Size, x.Quantity })
                .ToListAsync();
            if (items.Count == 0) return (false, "Đơn hàng không có sản phẩm.");

            var requirements = new Dictionary<int, decimal>();
            foreach (var item in items)
            {
                var recipe = await _context.Recipes.AsNoTracking()
                    .Include(x => x.RecipeItems)
                    .FirstOrDefaultAsync(x => x.MenuItemId == item.MenuItemId && x.Size == item.Size);
                if (recipe == null || recipe.RecipeItems.Count == 0)
                    return (false, $"Chưa có công thức cho đồ uống #{item.MenuItemId}, size {item.Size}.");

                foreach (var part in recipe.RecipeItems)
                {
                    if (part.RequiredAmount <= 0)
                        return (false, "Công thức có định lượng không hợp lệ.");
                    // Không tự đổi đơn vị: công thức và nguyên liệu phải thống nhất.
                    var ingredient = await _context.Ingredients.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.IngredientId == part.IngredientId);
                    if (ingredient == null || !string.Equals(ingredient.Unit.Trim(), part.Unit.Trim(), StringComparison.OrdinalIgnoreCase))
                        return (false, $"Đơn vị nguyên liệu #{part.IngredientId} không khớp công thức.");
                    var needed = part.RequiredAmount * item.Quantity;
                    requirements[part.IngredientId] =
                        requirements.GetValueOrDefault(part.IngredientId) + needed;
                }
            }

            foreach (var requirement in requirements.OrderBy(x => x.Key))
            {
                var ingredientId = requirement.Key;
                var needed = requirement.Value;
                // Cập nhật có điều kiện, không để OnHand - Reserved âm.
                var affected = await _context.Ingredients
                    .Where(x => x.IngredientId == ingredientId &&
                        x.OnHand - x.Reserved >= needed)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.OnHand, x => x.OnHand - needed));
                if (affected != 1)
                    return (false, $"Không đủ nguyên liệu #{ingredientId} hoặc kho vừa thay đổi.");
            }
            return (true, "");
        }
    }
}
