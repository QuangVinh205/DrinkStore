using DrinkStore.Models;
namespace DrinkStore.Areas.Admin.Models;
public class DashboardViewModel
{
    public DateTime Today { get; set; }
    public decimal Revenue { get; set; }
    public int OrdersToday { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveItems { get; set; }
    public int TotalItems { get; set; }
    public int Customers { get; set; }
    public List<Order> RecentOrders { get; set; } = [];
    public List<Ingredient> LowStock { get; set; } = [];
}
