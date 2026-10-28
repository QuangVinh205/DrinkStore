
namespace DrinkStore.Models
{
    public class Customer : User
    {
        public string Address { get; set; } = "";
        public string? IdentityUserId { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
