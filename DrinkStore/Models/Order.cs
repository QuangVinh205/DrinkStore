
namespace DrinkStore.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; }
            = DateTime.UtcNow;

        public OrderStatus Status { get; set; }
            = OrderStatus.Pending;

        public decimal Subtotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Total { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public string? CouponCode { get; set; }

        public Coupon? Coupon { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
