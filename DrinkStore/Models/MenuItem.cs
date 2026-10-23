
using System.ComponentModel.DataAnnotations;

namespace DrinkStore.Models
{
    public class MenuItem
    {
        public int ItemId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal BasePrice { get; set; }

        public bool Active { get; set; } = true;

        public string ImageUrl { get; set; } = "";

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
