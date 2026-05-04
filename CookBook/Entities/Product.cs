using CookBook.Domain.Enums;

namespace CookBook.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string>? Photos { get; set; }
    public float Calories { get; set; }
    public float Proteins { get; set; }
    public float Fats { get; set; }
    public float Carbs { get; set; }
    public string? Composition { get; set; }
    public ProductCategory Category { get; set; }
    public CookingRequired CookingRequired { get; set; }
    public ProductFlag Flags { get; set; } = ProductFlag.None;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DishProduct> DishProducts { get; set; } = new List<DishProduct>();
}