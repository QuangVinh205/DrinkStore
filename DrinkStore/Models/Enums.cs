
namespace DrinkStore.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparing,
        Completed,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
}
