using CookBook.Domain.Enums;

namespace CookBook.WebApi.DTOs;

public class UpdateDishRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Photos { get; set; }
    public float? Calories { get; set; }
    public float? Proteins { get; set; }
    public float? Fats { get; set; }
    public float? Carbs { get; set; }
    public float PortionSize { get; set; }
    public DishCategory? Category { get; set; }
    public List<DishProductDto> Composition { get; set; } = new();
}