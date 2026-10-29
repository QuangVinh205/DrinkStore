using DrinkStore.Controllers;
using DrinkStore.Data;
using DrinkStore.Extensions;
using DrinkStore.Models;
using DrinkStore.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Run with: dotnet run --project tests/DrinkStore.SmokeTests
// Session/controller regression checks; no SQL Server or external test packages required.
using var db = new DrinkStoreDb(new DbContextOptionsBuilder<DrinkStoreDb>().Options);
var session = new MemorySession();
var controller = new CartController(db)
{
    ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { Session = session }
    }
};
CartItemViewModel Item(string size, int quantity) => new()
{
    MenuItemId = 1, Size = size, Quantity = quantity, Price = 30000
};
void Seed(params CartItemViewModel[] items) => session.SetObject("Cart", items.ToList());
List<CartItemViewModel> Read() => session.GetObject<List<CartItemViewModel>>("Cart")!;
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}

Seed(Item("M", 2), Item("L", 3));
Check(controller.Update(1, 4, "M", "M") is RedirectToActionResult &&
    Read().Single(x => x.Size == "M").Quantity == 4 &&
    Read().Single(x => x.Size == "L").Quantity == 3, "Update only the selected size");
Check(controller.Remove(1, "M") is RedirectToActionResult &&
    Read().Count == 1 && Read()[0].Size == "L", "Remove preserves the other size");

Seed(Item("M", 2), Item("L", 3));
Check(controller.Update(1, 4, "M", "L") is RedirectToActionResult &&
    Read().Count == 1 && Read()[0].Size == "L" && Read()[0].Quantity == 7 &&
    Read()[0].Price == 35000 && Read()[0].TotalPrice == 245000,
    "Changing to an existing size merges quantities");
Seed(Item("M", 2), Item("L", 98));
Check(controller.Update(1, 2, "M", "L") is BadRequestObjectResult &&
    Read().Count == 2 && Read()[0].Quantity == 2 && Read()[1].Quantity == 98,
    "Merge above 99 leaves the cart unchanged");
Seed(Item("M", 2));
Check(controller.Update(1, 2, "M", "L") is RedirectToActionResult &&
    Read()[0].Size == "L" && Read()[0].Price == 35000 && Read()[0].TotalPrice == 70000,
    "Changing M to L adds 5000 per drink");
Check(controller.Update(1, 3, "L", "L") is RedirectToActionResult &&
    Read()[0].Price == 35000 && Read()[0].TotalPrice == 105000,
    "Updating quantity does not add the size surcharge again");
Check(controller.Update(1, 2, "L", "M") is RedirectToActionResult &&
    Read()[0].Price == 30000 && Read()[0].TotalPrice == 60000,
    "Changing L back to M restores the base price");
Check(controller.Update(1, 2, "M", "L") is RedirectToActionResult &&
    Read()[0].Price == 35000, "Selecting L again applies the surcharge once");
foreach (var quantity in new[] { 0, -1, 100 })
    Check(controller.Update(1, quantity, "L", "L") is BadRequestObjectResult &&
        Read()[0].Quantity == 2, $"Reject invalid quantity {quantity}");
Check(controller.Update(1, 2, "L", "S") is BadRequestObjectResult &&
    Read()[0].Size == "L", "Reject unsupported size");
Check(controller.Update(1, 2, "M", "L") is NotFoundResult,
    "Stale size does not update a different row");
Check(await controller.Add(1, 1, "S") is BadRequestObjectResult,
    "Reject unsupported size before querying the database");
controller.ModelState.AddModelError("quantity", "Invalid number");
Check(controller.Update(1, 2, "L", "L") is BadRequestObjectResult,
    "Reject model binding errors");
Check(DrinkSizes.All.SequenceEqual(new[] { "M", "L" }) &&
    !DrinkSizes.IsValid(null) && new CartItemViewModel().Size == DrinkSizes.Default,
    "Shared size options and default agree");
Check(DrinkSizes.GetPrice(30000m, "M") == 30000m &&
    DrinkSizes.GetPrice(30000m, "L") == 35000m,
    "Size pricing uses the base price plus 5000 for L");
Seed(Item("L", 2));
var legacyCart = (List<CartItemViewModel>)((ViewResult)controller.Index()).Model!;
Check(legacyCart[0].Price == 35000m && legacyCart[0].TotalPrice == 70000m,
    "Existing session carts receive size pricing");
Console.WriteLine($"{passed} checks passed.");

sealed class MemorySession : ISession
{
    private readonly Dictionary<string, byte[]> values = new();
    public bool IsAvailable => true;
    public string Id => "smoke-tests";
    public IEnumerable<string> Keys => values.Keys;
    public void Clear() => values.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => values.Remove(key);
    public void Set(string key, byte[] value) => values[key] = value;
    public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
}
