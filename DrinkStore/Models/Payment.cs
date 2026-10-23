
namespace DrinkStore.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string RequestKey { get; set; } = "";

        public string ProviderRef { get; set; } = "";
    }
}
