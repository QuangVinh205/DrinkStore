
namespace DrinkStore.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }

        public int MenuItemId { get; set; }

        public MenuItem MenuItem { get; set; } = null!;

        public string Size { get; set; } = "";

        public string Instructions { get; set; } = "";

        public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    }
}
