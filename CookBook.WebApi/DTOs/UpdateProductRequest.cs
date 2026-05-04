using CookBook.Domain.Enums;

namespace CookBook.WebApi.DTOs;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Photos { get; set; }
    public float Calories { get; set; }
    public float Proteins { get; set; }
    public float Fats { get; set; }
    public float Carbs { get; set; }
    public string? Composition { get; set; }
    public ProductCategory Category { get; set; }
    public CookingRequired CookingRequired { get; set; }
    public List<string>? Flags { get; set; }
}