namespace CookBook.Domain.Entities;

public class DishProduct
{
    public Guid Id { get; set; }
    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public float Amount { get; set; }
}