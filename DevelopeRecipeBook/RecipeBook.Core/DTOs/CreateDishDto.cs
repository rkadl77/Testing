namespace RecipeBook.Core.DTOs;

public class CreateDishDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public double? Calories { get; set; }                     // null — оставить авторасчёт
    public double? Proteins { get; set; }
    public double? Fats { get; set; }
    public double? Carbs { get; set; }
    public double PortionSize { get; set; }
    public string? Category { get; set; }                     // null или из макроса в названии
    public string Flags { get; set; } = string.Empty;
    public List<DishProductDto> Ingredients { get; set; } = new();  // состав
}

public class DishProductDto
{
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }                      // граммы в порции
}