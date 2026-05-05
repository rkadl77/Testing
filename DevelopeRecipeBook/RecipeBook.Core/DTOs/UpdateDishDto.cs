namespace RecipeBook.Core.DTOs;

public class UpdateDishDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public double? Calories { get; set; }
    public double? Proteins { get; set; }
    public double? Fats { get; set; }
    public double? Carbs { get; set; }
    public double PortionSize { get; set; }
    public string? Category { get; set; }
    public string Flags { get; set; } = string.Empty;
    public List<DishProductDto> Ingredients { get; set; } = new();
}