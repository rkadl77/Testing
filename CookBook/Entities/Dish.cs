using CookBook.Domain.Enums;

namespace CookBook.Domain.Entities;

public class Dish
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string>? Photos { get; set; }
    public float Calories { get; set; }
    public float Proteins { get; set; }
    public float Fats { get; set; }
    public float Carbs { get; set; }
    public float PortionSize { get; set; }
    public DishCategory Category { get; set; }
    public DishFlag Flags { get; set; } = DishFlag.None;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DishProduct> DishProducts { get; set; } = new List<DishProduct>();
}