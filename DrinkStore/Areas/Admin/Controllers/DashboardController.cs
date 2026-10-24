using DrinkStore.Areas.Admin.Models;
using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DrinkStore.Areas.Admin.Controllers;
[Area("Admin")]
public class DashboardController(DrinkStoreDb context) : Controller
{
    public async Task<IActionResult> Index()
    {
        // OrderDate is UTC; use the Vietnam business day.
        var today = DateTime.UtcNow.AddHours(7).Date;
        var start = DateTime.SpecifyKind(today.AddHours(-7), DateTimeKind.Utc);
        var end = start.AddDays(1);
        var orders = context.Orders.AsNoTracking();
        var todayOrders = orders.Where(x => x.OrderDate >= start && x.OrderDate < end);
        var model = new DashboardViewModel
        {
            Today = today,
            Revenue = await todayOrders.Where(x => x.Status == OrderStatus.Completed).SumAsync(x => (decimal?)x.Total) ?? 0,
            OrdersToday = await todayOrders.CountAsync(),
            PendingOrders = await orders.CountAsync(x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Confirmed || x.Status == OrderStatus.Preparing),
            ActiveItems = await context.MenuItems.CountAsync(x => x.Active),
            TotalItems = await context.MenuItems.CountAsync(),
            Customers = await context.Customers.CountAsync(),
            RecentOrders = await orders.Include(x => x.Customer).OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderId).Take(8).ToListAsync(),
            LowStock = await context.Ingredients.AsNoTracking().Where(x => x.OnHand - x.Reserved <= x.ReorderThreshold).OrderBy(x => x.OnHand - x.Reserved).ThenBy(x => x.Name).Take(5).ToListAsync()
        };
        return View(model);
    }
}
