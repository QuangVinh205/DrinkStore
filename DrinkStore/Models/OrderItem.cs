
namespace DrinkStore.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public int MenuItemId { get; set; }

        public MenuItem MenuItem { get; set; } = null!;

        public int Quantity { get; set; }

        public string Size { get; set; } = "";

        public decimal UnitPriceSnapshot { get; set; }

        public decimal GetSubtotal()
        {
            return Quantity * UnitPriceSnapshot;
        }
    }
}
