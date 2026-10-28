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
    public int Days { get; set; }
    public DateTime PeriodStart { get; set; }
    public decimal PeriodRevenue { get; set; }
    public int PeriodOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal AverageOrderValue => CompletedOrders == 0 ? 0 : PeriodRevenue / CompletedOrders;
    public List<DailySalesViewModel> DailySales { get; set; } = [];
    public List<OrderStatusCountViewModel> StatusCounts { get; set; } = [];
    public List<TopDrinkViewModel> TopDrinks { get; set; } = [];
    public List<Order> RecentOrders { get; set; } = [];
    public List<Ingredient> LowStock { get; set; } = [];
}

public class DailySalesViewModel
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public class OrderStatusCountViewModel
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class TopDrinkViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}
