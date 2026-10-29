using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DrinkStore.Areas.Admin.ViewModels;

public class RecipeIngredientInput
{
    public int IngredientId { get; set; }
    [Range(typeof(decimal), "0.001", "999999999", ErrorMessage = "Định lượng phải lớn hơn 0.")]
    public decimal RequiredAmount { get; set; }
}

public class RecipeFormViewModel
{
    public int RecipeId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Chọn đồ uống.")]
    public int MenuItemId { get; set; }
    [Required, RegularExpression(DrinkStore.Models.DrinkSizes.Pattern, ErrorMessage = "Size chỉ được M hoặc L.")]
    public string Size { get; set; } = DrinkStore.Models.DrinkSizes.Default;
    [StringLength(2000)]
    public string Instructions { get; set; } = "";
    public List<RecipeIngredientInput> Ingredients { get; set; } = new();
    public List<SelectListItem> MenuOptions { get; set; } = new();
    public List<SelectListItem> IngredientOptions { get; set; } = new();
}
