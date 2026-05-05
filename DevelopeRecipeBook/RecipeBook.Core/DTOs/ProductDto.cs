namespace RecipeBook.Core.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public double Calories { get; set; }
    public double Proteins { get; set; }
    public double Fats { get; set; }
    public double Carbs { get; set; }
    public string? Composition { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CookingRequirement { get; set; } = string.Empty;
    public string Flags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> UsedInDishes { get; set; } = new();  // названия блюд, где используется
}