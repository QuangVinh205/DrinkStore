
namespace DrinkStore.Models
{
    public class RecipeItem
    {
        public int RecipeItemId { get; set; }

        public int RecipeId { get; set; }

        public Recipe Recipe { get; set; } = null!;

        public int IngredientId { get; set; }

        public Ingredient Ingredient { get; set; } = null!;

        public decimal RequiredAmount { get; set; }

        public string Unit { get; set; } = "";
    }
}
