using DrinkStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Data;

// Called only after Program validates Development and the isolated demo database.
public static class DemoSeeder
{
    public const string ManagerEmail = "manager@drinkstore.example";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DrinkStoreDb>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var manager = await users.FindByEmailAsync(ManagerEmail);
        if (manager == null)
        {
            manager = new ApplicationUser
            {
                UserName = ManagerEmail, Email = ManagerEmail,
                FullName = "Quản lý Demo", EmailConfirmed = true
            };
            RequireSuccess(await users.CreateAsync(manager, configuration["Demo:ManagerPassword"]!));
        }
        if (!await users.IsInRoleAsync(manager, "ShiftManager"))
            RequireSuccess(await users.AddToRoleAsync(manager, "ShiftManager"));

        // Preserve edits and orders when restarting an existing demo.
        if (await db.MenuItems.AnyAsync()) return;

        var coffee = Ingredient("Cà phê", "g", 5000);
        var milk = Ingredient("Sữa", "ml", 20000);
        var matcha = Ingredient("Bột matcha", "g", 1000);
        var tea = Ingredient("Trà", "g", 3000);
        var peach = Ingredient("Syrup đào", "ml", 5000);

        db.MenuItems.AddRange(
            Drink("Cà phê sữa đá", "Cà phê đậm đà hòa cùng sữa, thơm ngon và tỉnh táo.", 30000, "caphesua.jpg", (coffee, 20), (milk, 50)),
            Drink("Matcha latte", "Matcha dịu nhẹ kết hợp sữa béo mịn.", 45000, "matcha.jpg", (matcha, 5), (milk, 150)),
            Drink("Trà đào cam sả", "Trà thơm, vị đào thanh ngọt cho ngày tươi mát.", 35000, "tradao.jpg", (tea, 10), (peach, 30)),
            Drink("Trà sữa", "Trà đậm vị cùng sữa ngọt dịu, dễ uống mỗi ngày.", 35000, "trasua.jpg", (tea, 10), (milk, 100)));
        await db.SaveChangesAsync();
    }

    private static Ingredient Ingredient(string name, string unit, decimal stock) => new()
    {
        Name = name, Unit = unit, OnHand = stock, ReorderThreshold = 100
    };

    private static MenuItem Drink(string name, string description, decimal price, string image,
        params (Ingredient Ingredient, decimal Amount)[] parts)
    {
        var item = new MenuItem
        {
            Name = name, Description = description, BasePrice = price,
            ImageUrl = "/images/" + image, Active = true
        };
        foreach (var size in DrinkSizes.All)
        {
            var recipe = new Recipe { Size = size, Instructions = "Pha theo định lượng, thêm đá và phục vụ." };
            foreach (var part in parts)
                recipe.RecipeItems.Add(new RecipeItem
                {
                    Ingredient = part.Ingredient, Unit = part.Ingredient.Unit,
                    RequiredAmount = part.Amount * (size == "L" ? 1.25m : 1m)
                });
            item.Recipes.Add(recipe);
        }
        return item;
    }

    private static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
