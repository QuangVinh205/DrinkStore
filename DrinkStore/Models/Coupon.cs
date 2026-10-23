
using System.ComponentModel.DataAnnotations;

namespace DrinkStore.Models
{
    public class Coupon
    {
        [Key]
        public string Code { get; set; } = "";

        public decimal Percentage { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public decimal MinOrderValue { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
