namespace RecipeBook.Core.DTOs;

public class DishDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public double Calories { get; set; }
    public double Proteins { get; set; }
    public double Fats { get; set; }
    public double Carbs { get; set; }
    public double PortionSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Flags { get; set; } = string.Empty;
    public List<DishIngredientDto> Ingredients { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DishIngredientDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
}