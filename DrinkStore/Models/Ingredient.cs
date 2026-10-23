
namespace DrinkStore.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }

        public string Name { get; set; } = "";

        public string Unit { get; set; } = "";

        public decimal OnHand { get; set; }

        public decimal Reserved { get; set; }

        public decimal ReorderThreshold { get; set; }

        public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    }
}
