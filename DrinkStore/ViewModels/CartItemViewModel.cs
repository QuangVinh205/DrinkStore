namespace DrinkStore.ViewModels
{
    public class CartItemViewModel
    {
        public int MenuItemId { get; set; }

        public string Name { get; set; } = "";

        public string ImageUrl { get; set; } = "";

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice => Price * Quantity;
    }
}