namespace RecipeBook.Core.Models;

public class DishProduct
{
    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public double Quantity { get; set; }
}