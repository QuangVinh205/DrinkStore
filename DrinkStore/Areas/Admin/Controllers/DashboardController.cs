using DrinkStore.Areas.Admin.Models;
using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DrinkStore.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "ShiftManager")]
public class DashboardController(DrinkStoreDb context) : Controller
{
    public async Task<IActionResult> Index(int days = 7)
    {
        if (days is not (7 or 30 or 90))
            return BadRequest("Khoảng thống kê không hợp lệ.");

        // OrderDate is UTC; use the Vietnam business day.
        var today = DateTime.UtcNow.AddHours(7).Date;
        var start = DateTime.SpecifyKind(today.AddHours(-7), DateTimeKind.Utc);
        var end = start.AddDays(1);
        var orders = context.Orders.AsNoTracking();
        var todayOrders = orders.Where(x => x.OrderDate >= start && x.OrderDate < end);
        var periodStart = start.AddDays(1 - days);
        var periodOrders = orders.Where(x => x.OrderDate >= periodStart && x.OrderDate < end);
        var dailySales = await periodOrders
            .Where(x => x.Status == OrderStatus.Completed)
            .GroupBy(x => x.OrderDate.AddHours(7).Date)
            .Select(group => new DailySalesViewModel
            {
                Date = group.Key,
                Revenue = group.Sum(x => x.Total),
                Orders = group.Count()
            }).ToListAsync();
        var statusCounts = await periodOrders.GroupBy(x => x.Status)
            .Select(group => new OrderStatusCountViewModel { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        var model = new DashboardViewModel
        {
            Days = days,
            PeriodStart = today.AddDays(1 - days),
            PeriodRevenue = dailySales.Sum(x => x.Revenue),
            PeriodOrders = statusCounts.Sum(x => x.Count),
            CompletedOrders = dailySales.Sum(x => x.Orders),
            // Include quiet days so gaps are not mistaken for missing data.
            DailySales = Enumerable.Range(0, days).Select(offset =>
            {
                var date = today.AddDays(1 - days + offset);
                return dailySales.FirstOrDefault(x => x.Date == date) ?? new DailySalesViewModel { Date = date };
            }).ToList(),
            StatusCounts = Enum.GetValues<OrderStatus>().Select(status =>
                statusCounts.FirstOrDefault(x => x.Status == status) ?? new OrderStatusCountViewModel { Status = status })
                .ToList(),
            TopDrinks = await context.OrderItems.AsNoTracking()
                .Where(x => x.Order.OrderDate >= periodStart && x.Order.OrderDate < end && x.Order.Status == OrderStatus.Completed)
                .GroupBy(x => new { x.MenuItemId, x.MenuItem.Name })
                .Select(group => new TopDrinkViewModel
                {
                    MenuItemId = group.Key.MenuItemId,
                    Name = group.Key.Name,
                    Quantity = group.Sum(x => x.Quantity),
                    Subtotal = group.Sum(x => x.Quantity * x.UnitPriceSnapshot)
                })
                .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Subtotal).ThenBy(x => x.MenuItemId)
                .Take(5).ToListAsync(),
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
