using CookBook.Domain.Enums;

namespace CookBook.WebApi.DTOs;

public class CreateDishRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Photos { get; set; }
    public float PortionSize { get; set; }
    public DishCategory? Category { get; set; }
    public List<DishProductDto> Composition { get; set; } = new();
}

public class DishProductDto
{
    public Guid ProductId { get; set; }
    public float Amount { get; set; }
}