namespace RecipeBook.Core.DTOs;

public class UpdateProductDto
{
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
}